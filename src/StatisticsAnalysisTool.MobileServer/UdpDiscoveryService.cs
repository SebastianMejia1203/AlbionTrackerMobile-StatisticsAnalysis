using Serilog;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace StatisticsAnalysisTool.MobileServer;

/// <summary>
/// Listens on UDP port 7778 for mobile-client broadcast discovery packets.
/// When it receives "ALBION_SAT_DISCOVER", it replies with JSON containing
/// the server's real LAN IP and HTTP port so the mobile app can connect.
/// </summary>
public class UdpDiscoveryService : IDisposable
{
    public const int DiscoveryPort = 7778;
    private const string DiscoveryRequest = "ALBION_SAT_DISCOVER";

    private readonly int _httpPort;
    private UdpClient? _udpClient;
    private CancellationTokenSource? _cts;

    public UdpDiscoveryService(int httpPort)
    {
        _httpPort = httpPort;
    }

    public void Start()
    {
        try
        {
            _cts = new CancellationTokenSource();
            _udpClient = new UdpClient();
            _udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, DiscoveryPort));
            _udpClient.EnableBroadcast = true;

            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
            Log.Information("UDP Discovery service listening on port {Port}", DiscoveryPort);
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Could not start UDP Discovery service on port {Port}", DiscoveryPort);
        }
    }

    private async Task ListenLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var result = await _udpClient!.ReceiveAsync(ct);
                var message = Encoding.UTF8.GetString(result.Buffer).Trim();

                if (message != DiscoveryRequest) continue;

                var localIp = GetBestLocalIp();
                var response = JsonSerializer.Serialize(new
                {
                    name = "SAT Mobile Server",
                    host = localIp,
                    port = _httpPort
                });

                var bytes = Encoding.UTF8.GetBytes(response);
                await _udpClient.SendAsync(bytes, bytes.Length, result.RemoteEndPoint);
                Log.Debug("Discovery: replied to {Endpoint} — host={Host}, port={Port}",
                    result.RemoteEndPoint, localIp, _httpPort);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                if (!ct.IsCancellationRequested)
                    Log.Warning(ex, "UDP Discovery receive error");
            }
        }
    }

    /// <summary>
    /// Returns the best LAN IP for this machine.
    /// Prefers 192.168.x.x (home/office router), then 10.x.x.x, then any other private IP.
    /// Skips virtual adapters (Hyper-V, VirtualBox, etc.) that start with 172.
    /// </summary>
    private static string GetBestLocalIp()
    {
        string? tenNet = null;
        string? other = null;

        foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (ni.OperationalStatus != OperationalStatus.Up) continue;
            if (ni.NetworkInterfaceType is NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel) continue;

            // Skip virtual adapters (Hyper-V Default Switch, etc.)
            var name = ni.Name + ni.Description;
            if (name.Contains("vEthernet", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("VirtualBox", StringComparison.OrdinalIgnoreCase) ||
                name.Contains("VMware", StringComparison.OrdinalIgnoreCase)) continue;

            foreach (var addr in ni.GetIPProperties().UnicastAddresses)
            {
                if (addr.Address.AddressFamily != AddressFamily.InterNetwork) continue;
                if (IPAddress.IsLoopback(addr.Address)) continue;

                var ip = addr.Address.ToString();
                if (ip.StartsWith("192.168.")) return ip;      // Best: home router
                if (ip.StartsWith("10.")) tenNet ??= ip;       // Good: corporate
                else other ??= ip;                              // Fallback
            }
        }

        return tenNet ?? other ?? "127.0.0.1";
    }

    public void Stop()
    {
        _cts?.Cancel();
        try { _udpClient?.Close(); } catch { /* ignored */ }
        _udpClient?.Dispose();
        _udpClient = null;
    }

    public void Dispose() => Stop();
}
