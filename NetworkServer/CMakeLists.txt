cmake_minimum_required(VERSION 3.20)

project(NetworkServer LANGUAGES CXX)

include(CTest)

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)

add_executable(NetworkServer
    src/main.cpp

    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/TcpListener.h
    src/net/TcpListener.cpp
    src/net/SelectLoop.h
    src/net/SelectLoop.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp
    src/net/UdpSocket.h
    src/net/UdpSocket.cpp
    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/ServerLoop.h
    src/net/ServerLoop.cpp
    src/net/UdpEndpoint.h
    src/net/UdpBindHandler.h
    src/net/UdpBindHandler.cpp
    src/net/UdpPingHandler.h
    src/net/UdpPingHandler.cpp

    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/UdpMessageHeader.h
    src/protocol/UdpPacketCodec.h
    src/protocol/UdpPacketCodec.cpp
    src/protocol/SessionTokenCodec.h
    src/protocol/SessionTokenCodec.cpp
    src/protocol/UdpBindingCredentialsCodec.h
    src/protocol/UdpBindingCredentialsCodec.cpp
    src/protocol/UdpSequenceTracker.h

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp
)

target_include_directories(NetworkServer
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(NetworkServer
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

   target_link_libraries(NetworkServer
        PRIVATE
            ws2_32
            bcrypt
    )
endif()

if (BUILD_TESTING)
    add_executable(PacketCodecTests
        tests/PacketCodecTests.cpp
        src/protocol/PacketCodec.cpp
        src/protocol/PacketCodec.h
        src/protocol/MessageType.h
        src/protocol/MessageHeader.h
        src/protocol/NetworkByteOrder.h
    )

    target_include_directories(PacketCodecTests
        PRIVATE
            src
    )

    if (WIN32)
        target_compile_definitions(PacketCodecTests
            PRIVATE
                WIN32_LEAN_AND_MEAN
                NOMINMAX
        )

        target_link_libraries(PacketCodecTests
            PRIVATE
                ws2_32
                bcrypt
    )
    endif()

    add_test(
        NAME PacketCodecTests
        COMMAND PacketCodecTests
    )

    add_executable(TcpListenerTests
    	tests/TcpListenerTests.cpp
    	src/net/SocketRuntime.h
    	src/net/SocketRuntime.cpp
    	src/net/TcpListener.h
    	src/net/TcpListener.cpp
        src/net/TcpConnection.h
	src/net/TcpConnection.cpp
)

target_include_directories(TcpListenerTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(TcpListenerTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(TcpListenerTests
        PRIVATE
            ws2_32
	    bcrypt
    )
endif()

add_test(
    NAME TcpListenerTests
    COMMAND TcpListenerTests
)

add_executable(SelectLoopTests
    tests/SelectLoopTests.cpp

    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/TcpListener.h
    src/net/TcpListener.cpp
    src/net/SelectLoop.h
    src/net/SelectLoop.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
)

target_include_directories(SelectLoopTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(SelectLoopTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(SelectLoopTests
        PRIVATE
            ws2_32
	    bcrypt
    )
endif()

add_test(
    NAME SelectLoopTests
    COMMAND SelectLoopTests
)

add_executable(NicknameMessageTests
    tests/NicknameMessageTests.cpp
    src/protocol/PacketCodec.cpp
    src/protocol/PacketCodec.h
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
)

target_include_directories(NicknameMessageTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(NicknameMessageTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(NicknameMessageTests
        PRIVATE
            ws2_32
       	    bcrypt
    )
endif()

add_test(
    NAME NicknameMessageTests
    COMMAND NicknameMessageTests
)

add_executable(ClientStateTests
    tests/ClientStateTests.cpp

    src/protocol/PacketCodec.cpp
    src/protocol/PacketCodec.h
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h

    src/room/ClientState.h
    src/room/ClientState.cpp
)

target_include_directories(ClientStateTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(ClientStateTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(ClientStateTests
        PRIVATE
            ws2_32
	    bcrypt
    )
endif()

add_test(
    NAME ClientStateTests
    COMMAND ClientStateTests
)

add_executable(TcpClientSessionTests
    tests/TcpClientSessionTests.cpp

    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp

    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/UdpMessageHeader.h
    src/protocol/SessionTokenCodec.h
    src/protocol/SessionTokenCodec.cpp
)

target_include_directories(TcpClientSessionTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(TcpClientSessionTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(TcpClientSessionTests
        PRIVATE
            ws2_32
	    bcrypt
    )
endif()

add_test(
    NAME TcpClientSessionTests
    COMMAND TcpClientSessionTests
)

add_executable(RoomManagerTests
    tests/RoomManagerTests.cpp

    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/Room.h
    src/room/Room.cpp
)

target_include_directories(RoomManagerTests
    PRIVATE
        src
)

add_test(
    NAME RoomManagerTests
    COMMAND RoomManagerTests
)

add_executable(PlayerIdAllocatorTests
    tests/PlayerIdAllocatorTests.cpp

    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
)

target_include_directories(PlayerIdAllocatorTests
    PRIVATE
        src
)

add_test(
    NAME PlayerIdAllocatorTests
    COMMAND PlayerIdAllocatorTests
)

add_executable(SessionTokenGeneratorTests
    tests/SessionTokenGeneratorTests.cpp

    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp
)

target_include_directories(SessionTokenGeneratorTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(SessionTokenGeneratorTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(SessionTokenGeneratorTests
        PRIVATE
            bcrypt
    )
endif()

add_test(
    NAME SessionTokenGeneratorTests
    COMMAND SessionTokenGeneratorTests
)

add_executable(UdpSocketTests
    tests/UdpSocketTests.cpp

    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/UdpSocket.h
    src/net/UdpSocket.cpp
)

target_include_directories(UdpSocketTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(UdpSocketTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(UdpSocketTests
        PRIVATE
            ws2_32
    )
endif()

add_test(
    NAME UdpSocketTests
    COMMAND UdpSocketTests
)

add_executable(ServerCoordinatorTests
    tests/ServerCoordinatorTests.cpp

    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp
    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
    src/net/UdpBindHandler.h
    src/net/UdpBindHandler.cpp

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp

    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/SessionTokenCodec.cpp
    src/protocol/UdpMessageHeader.h
    src/protocol/UdpPacketCodec.h
    src/protocol/UdpPacketCodec.cpp
)

target_include_directories(ServerCoordinatorTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(ServerCoordinatorTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(ServerCoordinatorTests
        PRIVATE
            ws2_32
            bcrypt
    )
endif()

add_test(
    NAME ServerCoordinatorTests
    COMMAND ServerCoordinatorTests
)

add_executable(ServerLoopTests
    tests/ServerLoopTests.cpp

    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp
    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/TcpListener.h
    src/net/TcpListener.cpp
    src/net/SelectLoop.h
    src/net/SelectLoop.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
    src/net/ServerLoop.h
    src/net/ServerLoop.cpp
    src/net/UdpSocket.h
    src/net/UdpSocket.cpp
    src/net/UdpBindHandler.h
    src/net/UdpBindHandler.cpp
    src/net/UdpPingHandler.h
    src/net/UdpPingHandler.cpp

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp

    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/SessionTokenCodec.cpp
    src/protocol/UdpBindingCredentialsCodec.h
    src/protocol/UdpBindingCredentialsCodec.cpp
    src/protocol/UdpSequenceTracker.h
    src/protocol/UdpMessageHeader.h
    src/protocol/UdpPacketCodec.h
    src/protocol/UdpPacketCodec.cpp
)

target_include_directories(ServerLoopTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(ServerLoopTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(ServerLoopTests
        PRIVATE
            ws2_32
            bcrypt
    )
endif()

add_test(
    NAME ServerLoopTests
    COMMAND ServerLoopTests
)

add_executable(ServerPacketSendTests
    tests/ServerPacketSendTests.cpp

    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
    src/net/TcpListener.h
    src/net/TcpListener.cpp
    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp

    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/SessionTokenCodec.cpp
)

target_include_directories(ServerPacketSendTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(ServerPacketSendTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(ServerPacketSendTests
        PRIVATE
            ws2_32
            bcrypt
    )
endif()

add_test(
    NAME ServerPacketSendTests
    COMMAND ServerPacketSendTests
)

add_executable(UdpProtocolTests
    tests/UdpProtocolTests.cpp

    src/protocol/MessageType.h
    src/protocol/UdpMessageHeader.h
    src/protocol/UdpPacketCodec.h
    src/protocol/UdpPacketCodec.cpp
    src/protocol/NetworkByteOrder.h
    src/protocol/UdpBindingCredentialsCodec.h
    src/protocol/UdpBindingCredentialsCodec.cpp
    src/protocol/UdpSequenceTracker.h
)

target_include_directories(UdpProtocolTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(UdpProtocolTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(UdpProtocolTests
        PRIVATE
            ws2_32
    )
endif()

add_test(
    NAME UdpProtocolTests
    COMMAND UdpProtocolTests
)

add_executable(UdpPingHandlerTests
    tests/UdpPingHandlerTests.cpp

    src/net/ServerCoordinator.h
    src/net/ServerCoordinator.cpp
    src/net/TcpClientSession.h
    src/net/TcpClientSession.cpp
    src/net/TcpConnection.h
    src/net/TcpConnection.cpp
    src/net/SocketRuntime.h
    src/net/SocketRuntime.cpp
    src/net/UdpPingHandler.h
    src/net/UdpPingHandler.cpp

    src/room/ClientState.h
    src/room/ClientState.cpp
    src/room/Room.h
    src/room/Room.cpp
    src/room/RoomManager.h
    src/room/RoomManager.cpp
    src/room/PlayerIdAllocator.h
    src/room/PlayerIdAllocator.cpp
    src/room/SessionTokenGenerator.h
    src/room/SessionTokenGenerator.cpp

    src/protocol/PacketCodec.h
    src/protocol/PacketCodec.cpp
    src/protocol/MessageType.h
    src/protocol/MessageHeader.h
    src/protocol/NetworkByteOrder.h
    src/protocol/SessionTokenCodec.cpp
    src/protocol/UdpMessageHeader.h
    src/protocol/UdpPacketCodec.h
    src/protocol/UdpPacketCodec.cpp
    src/protocol/UdpSequenceTracker.h
)

target_include_directories(UdpPingHandlerTests
    PRIVATE
        src
)

if (WIN32)
    target_compile_definitions(UdpPingHandlerTests
        PRIVATE
            WIN32_LEAN_AND_MEAN
            NOMINMAX
    )

    target_link_libraries(UdpPingHandlerTests
        PRIVATE
            ws2_32
            bcrypt
    )
endif()

add_test(
    NAME UdpPingHandlerTests
    COMMAND UdpPingHandlerTests
)

add_executable(SessionTokenCodecTests
    tests/SessionTokenCodecTests.cpp
    src/protocol/SessionTokenCodec.h
    src/protocol/SessionTokenCodec.cpp
    src/protocol/UdpMessageHeader.h
)

target_include_directories(SessionTokenCodecTests
    PRIVATE
        src
)

add_test(
    NAME SessionTokenCodecTests
    COMMAND SessionTokenCodecTests
)
endif()
