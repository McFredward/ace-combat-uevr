#include <winsock.h>
#include <cstring>
#include <iostream>

#include "uevr/API.hpp"

typedef SSIZE_T ssize_t; // Windows equivalent of ssize_t

class TelemetrySender
{
  private:
	int sockfd;
	struct sockaddr_in serverAddr;
	bool bIsInitialized;

  public:
	TelemetrySender() : sockfd(-1), bIsInitialized(false)
	{
	}

	~TelemetrySender()
	{
		if(sockfd >= 0) {
			closesocket(sockfd);
		}
	}

	bool Initialize(const char *ipAddress, int port)
	{
		// Create socket
		sockfd = socket(AF_INET, SOCK_DGRAM, 0); // UDP socket
		if(sockfd < 0) {
			uevr::API::get()->log_info("Socket creation failed!\n");
			return false;
		}

		// Configure server address
		memset(&serverAddr, 0, sizeof(serverAddr));
		serverAddr.sin_family = AF_INET;
		serverAddr.sin_port = htons(port);
		serverAddr.sin_addr.s_addr = inet_addr(ipAddress); // Set destination IP

		bIsInitialized = true;
		return true;
	}

	void SendTelemetryData(const UEVR_Rotatorf &rotation, WORD last_rumble_left,
						   WORD last_rumble_right)
	{
		if(!bIsInitialized) {
			uevr::API::get()->log_info("TelemetrySender is not initialized!\n");
			return;
		}

		// Serialize the data
		float data[5] = {rotation.pitch, rotation.yaw, rotation.roll, (float)last_rumble_left,
						 (float)last_rumble_right};

		// Send the data (cast data to const char*)
		ssize_t bytesSent = sendto(sockfd, reinterpret_cast<const char *>(data), sizeof(data), 0,
								   (sockaddr *)&serverAddr, sizeof(serverAddr));

		if(bytesSent == SOCKET_ERROR) {
			uevr::API::get()->log_info("Failed to send data!\n");
		}
	}
};
