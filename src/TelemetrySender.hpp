#include <winsock.h>
#include <cstring>
#include <iostream>
#include <cstdio>     // For snprintf

#include "uevr/API.hpp"

typedef SSIZE_T ssize_t; // Windows equivalent of ssize_t

class TelemetrySender
{
  private:
	int sockfd;
	struct sockaddr_in serverAddr;
	bool bIsInitialized;
	int failureCount;

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
			uevr::API::get()->log_info("Trying to initialize TelemetrySender...\n");

			for(int attempts = 0; attempts < 3; ++attempts) {
				try {
					// Create socket
					sockfd = socket(AF_INET, SOCK_DGRAM, 0); // UDP socket
					if(sockfd < 0) {
						uevr::API::get()->log_info("Socket creation failed!\n");
						perror("Socket creation error");
						return false;
					}

					// Configure server address
					memset(&serverAddr, 0, sizeof(serverAddr));
					serverAddr.sin_family = AF_INET;
					serverAddr.sin_port = htons(port);
					serverAddr.sin_addr.s_addr = inet_addr(ipAddress); 

					bIsInitialized = true;
					uevr::API::get()->log_info("TelemetrySender initialized successfully!\n");
					return true;

				} catch(const std::exception &e) {
					char buffer[256];
					snprintf(buffer, sizeof(buffer), "Exception occurred during initialization: %s",
							 e.what());
					uevr::API::get()->log_info(buffer);
					if(sockfd >= 0) {
						closesocket(sockfd); // Ensure the socket is closed in case of errors
					}
					return false;
				} catch(...) {
					uevr::API::get()->log_info("Unknown exception occurred during initialization.\n");
					if(sockfd >= 0) {
						closesocket(sockfd); // Ensure the socket is closed in case of errors
					}
					return false;
				}
			}

			// If all attempts fail
			uevr::API::get()->log_info(
				"Failed to initialize TelemetrySender after multiple attempts.\n");
			return false;
		}

	
	void SendTelemetryData(const UEVR_Rotatorf &rotation, WORD last_rumble_left,
							   WORD last_rumble_right)
		{
			if(!bIsInitialized) {
				//uevr::API::get()->log_info("TelemetrySender is not initialized!\n");
				return;
			}

			// Serialize the data
			float data[5] = {rotation.pitch, rotation.yaw, rotation.roll, (float)last_rumble_left,
							 (float)last_rumble_right};

			// Send the data (cast data to const char*)
			ssize_t bytesSent = sendto(sockfd, reinterpret_cast<const char *>(data), sizeof(data),0, (sockaddr *)&serverAddr, sizeof(serverAddr));

			if(bytesSent == SOCKET_ERROR) {
				failureCount++; // Increment the failure counter

				// Fetch and log the specific error code
				int errorCode = WSAGetLastError();

				// Log failure details
				uevr::API::get()->log_info("Failed to send data! Error code: %d\n", errorCode);
				uevr::API::get()->log_info(
					"Socket Descriptor: %d, Data Size: %llu bytes, Failure Count: %d\n", sockfd,
					sizeof(data), failureCount);

				// Check if failures have reached the threshold
				if(failureCount >= 5) {
					uevr::API::get()->log_info("Too many failures. Closing socket.\n");
					closesocket(sockfd); // Close the socket
					sockfd = -1;		 // Invalidate the socket descriptor
					bIsInitialized = false;
				}
			} else {
				failureCount = 0; // Reset the failure counter on success
				/*
				char buffer[256];
				snprintf(buffer, sizeof(buffer),
						 "Data sent: pitch=%f, yaw=%f, roll=%f, rumble_left=%f, rumble_right=%f\n",
						 data[0], data[1], data[2], data[3], data[4]);
				uevr::API::get()->log_info(buffer);
				*/
			}
		}
};
