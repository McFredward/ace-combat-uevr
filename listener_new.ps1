# Open a UDP client on port 20777 with socket reuse enabled
# Create UDP client
$udpClient = New-Object System.Net.Sockets.UdpClient
# Disable exclusive address use
$udpClient.Client.SetSocketOption([System.Net.Sockets.SocketOptionLevel]::Socket, [System.Net.Sockets.SocketOptionName]::ReuseAddress, $true)
# Bind to the port
$udpClient.Client.ExclusiveAddressUse = $false
$udpClient.Client.Bind((New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 20777)))

$endpoint = New-Object System.Net.IPEndPoint([System.Net.IPAddress]::Any, 0)

Write-Host "Listening on UDP port 20777..."

# Loop to continuously receive and process data
while ($true) {
    # Check if data is available
    if ($udpClient.Available -gt 0) {
        # Receive the UDP data
        $data = $udpClient.Receive([ref]$endpoint)

        # Check if the received data matches the expected size for 5 floats
        if ($data.Length -eq (5 * 4)) { # 5 floats, 4 bytes each
            # Convert the binary data into floats
            $floatArray = @()
            for ($i = 0; $i -lt 5; $i++) {
                $float = [BitConverter]::ToSingle($data, $i * 4)
                $floatArray += $float
            }
            Write-Host "Received floats: $($floatArray -join ', ')"
        } else {
            Write-Host "Unexpected data size: $($data.Length) bytes"
        }
    } else {
        Write-Host "<Null>"
    }
    Start-Sleep -Milliseconds 5 # Small delay to avoid tight polling
}
