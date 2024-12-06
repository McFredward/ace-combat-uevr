#include <winsock2.h>
#include <cstring>
#include <iostream>
#include <cstdio>     // For snprintf
#include <atomic>

#include "uevr/API.hpp"

typedef SSIZE_T ssize_t; // Windows equivalent of ssize_t

struct TelemetryData
{
	UEVR_Rotatorf rotation;
	WORD rumbleLeft;
	WORD rumbleRight;
};

class TelemetrySender
{
  private:
	int sockfd;
	struct sockaddr_in serverAddr;
	bool bIsInitialized;
	int failureCount;
	std::atomic<bool> threadRunning;

	std::thread workerThread;
	// Use double buffer method to avoid race conditions: Update values are done in buffer one rading in buffer two and swapping is done atomically
	std::atomic<TelemetryData *> sharedDataPtr;
	TelemetryData buffer1, buffer2;
	
	void SendTelemetryData(const UEVR_Rotatorf &rotation, WORD last_rumble_left,WORD last_rumble_right)
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

			// Log what was actually sent

			char buffer[256];
			snprintf(buffer, sizeof(buffer), "Data sent: ");
			for(size_t i = 0; i < 5; ++i) {
				char temp[32];
				snprintf(temp, sizeof(temp), "%f%s", data[i],
							(i < 5 - 1) ? ", " : "\n");
				strncat(buffer, temp, sizeof(buffer) - strlen(buffer) - 1);
			}

			uevr::API::get()->log_info(buffer);
			
			
		}
	}

	// For the use in their own thread
	void workerMethod()
	{
		uevr::API::get()->log_info("Starting sending thread.\n");
		auto nextTime = std::chrono::steady_clock::now();
		while(threadRunning) {
			nextTime += std::chrono::milliseconds(20);
			TelemetryData *localData = sharedDataPtr.load();
			SendTelemetryData(localData->rotation, localData->rumbleLeft, localData->rumbleRight);
			std::this_thread::sleep_until(nextTime);
		}
		uevr::API::get()->log_info("Exiting sending thread.\n");
	}

  public:
	TelemetrySender() : sockfd(-1), bIsInitialized(false), threadRunning(false)
	{
	}

	~TelemetrySender()
	{
		threadRunning = false;
		if(workerThread.joinable())
			workerThread.join();
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


				// Enable non-blocking mode
				u_long mode = 1; // 1 to enable non-blocking mode
				if(ioctlsocket(sockfd, FIONBIO, &mode) != 0) {
					int errorCode = WSAGetLastError();
					uevr::API::get()->log_info("Failed to set non-blocking mode. Error code: %d\n",
											   errorCode);
					closesocket(sockfd);
					sockfd = -1;
					return false;
				}

				// Configure server address
				memset(&serverAddr, 0, sizeof(serverAddr));
				serverAddr.sin_family = AF_INET;
				serverAddr.sin_port = htons(port);
				serverAddr.sin_addr.s_addr = inet_addr(ipAddress);

				bIsInitialized = true;
				uevr::API::get()->log_info("TelemetrySender initialized successfully!\n");

				// Init the first buffer with zero values
				TelemetryData all_zero_telemtry;
				memset(&all_zero_telemtry, 0, sizeof(all_zero_telemtry));
				updateData(all_zero_telemtry);

				// Start Thread
				start_worker();

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

	void updateData(const UEVR_Rotatorf &rotation, WORD rumbleLeft, WORD rumbleRight)
	{
		TelemetryData *newData =
			(sharedDataPtr.load() == &buffer1) ? &buffer2 : &buffer1; // Switch buffer
		newData->rotation = rotation;
		newData->rumbleLeft = rumbleLeft;
		newData->rumbleRight = rumbleRight;
		sharedDataPtr.store(newData); // Atomic pointer swap
	}

	void updateData(TelemetryData data)
	{
		updateData(data.rotation, data.rumbleLeft, data.rumbleRight);
	}

	void start_worker()
	{
		threadRunning = true;
		workerThread = std::thread(&TelemetrySender::workerMethod, this);
	}


};
