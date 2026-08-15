#include "room/RoomManager.h"
#include "room/Room.h"

#include <iostream>
#include <stdexcept>

int main()
{
    try
    {
        tdr::room::RoomManager roomManager;

        const auto createdRoom =
            roomManager.CreateRoom(
                1,
                "Seele"
            );

        roomManager.AddPlayer(
            createdRoom.Id(),
            2,
            "Bronya"
        );

        auto& room =
            roomManager.FindRoom(
                createdRoom.Id()
            );

        if (room.PlayerCount() != 2)
        {
            std::cerr
                << "[FAIL] Room does not contain "
                << "two players."
                << std::endl;

            return 1;
        }

        bool rejectedFullRoom = false;

        try
        {
            roomManager.AddPlayer(
                createdRoom.Id(),
                3,
                "Kiana"
            );
        }
        catch (const std::runtime_error&)
        {
            rejectedFullRoom = true;
        }

        if (!rejectedFullRoom)
        {
            std::cerr
                << "[FAIL] A third player joined "
                << "the full room."
                << std::endl;

            return 1;
        }

        if (room.PlayerAt(1).playerId != 2
            || room.PlayerAt(1).nickname != "Bronya"
            || room.PlayerAt(1).isHost)
        {
            std::cerr
                << "[FAIL] Joined player state is incorrect."
                << std::endl;

            return 1;
        }

        room.SetPlayerCharacter(
            1,
            tdr::room::CharacterId::Ranged
        );

        room.SetPlayerCharacter(
            2,
            tdr::room::CharacterId::Ranged
        );

        if (room.PlayerAt(0).selectedCharacter
            != tdr::room::CharacterId::Ranged
            || room.PlayerAt(1).selectedCharacter
            != tdr::room::CharacterId::Ranged)
        {
            std::cerr
                << "[FAIL] Room did not store "
                << "the player character selections."
                << std::endl;

            return 1;
        }

        room.SetPlayerReady(
            1,
            true
        );

        room.SetPlayerReady(
            2,
            true
        );

        if (!room.PlayerAt(0).isReady
            || !room.PlayerAt(1).isReady)
        {
            std::cerr
                << "[FAIL] Room did not store "
                << "the ready states."
                << std::endl;

            return 1;
        }

        room.SetPlayerCharacter(
            2,
            tdr::room::CharacterId::Melee
        );

        if (room.PlayerAt(1).isReady)
        {
            std::cerr
                << "[FAIL] Changing character did not "
                << "clear the player's ready state."
                << std::endl;

            return 1;
        }

        tdr::room::Room unselectedRoom(
            "UNSELECTED",
            10,
            "TestPlayer"
        );

        bool rejectedMissingCharacter = false;

        try
        {
            unselectedRoom.SetPlayerReady(
                10,
                true
            );
        }
        catch (const std::runtime_error&)
        {
            rejectedMissingCharacter = true;
        }

        if (!rejectedMissingCharacter)
        {
            std::cerr
                << "[FAIL] Player became ready "
                << "without selecting a character."
                << std::endl;

            return 1;
        }

        room.SetDifficulty(
            1,
            tdr::room::DifficultyId::Normal
        );

        if (room.SelectedDifficulty()
            != tdr::room::DifficultyId::Normal)
        {
            std::cerr
                << "[FAIL] Host difficulty selection "
                << "was not stored."
                << std::endl;

            return 1;
        }

        bool rejectedGuestDifficulty = false;

        try
        {
            room.SetDifficulty(
                2,
                tdr::room::DifficultyId::Hard
            );
        }
        catch (const std::runtime_error&)
        {
            rejectedGuestDifficulty = true;
        }

        if (!rejectedGuestDifficulty)
        {
            std::cerr
                << "[FAIL] Non-host player changed "
                << "the room difficulty."
                << std::endl;

            return 1;
        }

        if (room.SelectedDifficulty()
            != tdr::room::DifficultyId::Normal)
        {
            std::cerr
                << "[FAIL] Rejected difficulty request "
                << "changed the room state."
                << std::endl;

            return 1;
        }

        if (room.CanStart())
        {
            std::cerr
                << "[FAIL] Room can start while "
                << "a player is not ready."
                << std::endl;

            return 1;
        }

        room.SetPlayerReady(
            2,
            true
        );

        if (!room.CanStart())
        {
            std::cerr
                << "[FAIL] Room cannot start after "
                << "all requirements are satisfied."
                << std::endl;

            return 1;
        }

        bool rejectedGuestStart = false;

        try
        {
            room.Start(
                2
            );
        }
        catch (const std::runtime_error&)
        {
            rejectedGuestStart = true;
        }

        if (!rejectedGuestStart)
        {
            std::cerr
                << "[FAIL] Non-host player started "
                << "the room."
                << std::endl;

            return 1;
        }

        if (room.Status()
            != tdr::room::RoomStatus::Waiting)
        {
            std::cerr
                << "[FAIL] Rejected start request "
                << "changed the room status."
                << std::endl;

            return 1;
        }

        room.Start(
            1
        );

        bool rejectedStartedRoomJoin = false;

        try
        {
            roomManager.AddPlayer(
                createdRoom.Id(),
                3,
                "Kiana"
            );
        }
        catch (const std::runtime_error&)
        {
            rejectedStartedRoomJoin = true;
        }

        if (!rejectedStartedRoomJoin)
        {
            std::cerr
                << "[FAIL] A player joined "
                << "a started room."
                << std::endl;

            return 1;
        }

        if (room.Status()
            != tdr::room::RoomStatus::Started)
        {
            std::cerr
                << "[FAIL] Host start request did not "
                << "change the room status."
                << std::endl;

            return 1;
        }

        tdr::room::RoomManager leaveRoomManager;

        const auto leaveRoom =
            leaveRoomManager.CreateRoom(
                10,
                "Host"
            );

        const std::string leaveRoomId =
            leaveRoom.Id();

        leaveRoomManager.AddPlayer(
            leaveRoomId,
            20,
            "Guest"
        );

        leaveRoomManager.RemovePlayer(
            leaveRoomId,
            20
        );

        const auto& roomAfterGuestLeave =
            leaveRoomManager.FindRoom(
                leaveRoomId
            );

        if (roomAfterGuestLeave.PlayerCount() != 1)
        {
            std::cerr
                << "[FAIL] Guest leave did not remove "
                << "exactly one player."
                << std::endl;

            return 1;
        }

        if (roomAfterGuestLeave.PlayerAt(0).playerId
            != 10)
        {
            std::cerr
                << "[FAIL] Guest leave removed "
                << "the wrong player."
                << std::endl;

            return 1;
        }

        leaveRoomManager.RemovePlayer(
            leaveRoomId,
            10
        );

        bool roomWasRemoved = false;

        try
        {
            const auto& removedRoom =
                leaveRoomManager.FindRoom(
                    leaveRoomId
                );

            (void)removedRoom;
        }
        catch (const std::out_of_range&)
        {
            roomWasRemoved = true;
        }

        if (!roomWasRemoved)
        {
            std::cerr
                << "[FAIL] Host leave did not "
                << "remove the room."
                << std::endl;

            return 1;
        }

        std::cout
            << "[PASS] Room rules include "
            << "guest leave and host shutdown."
            << std::endl;

        return 0;
    }
    catch (const std::exception& exception)
    {
        std::cerr
            << "[FAIL] "
            << exception.what()
            << std::endl;

        return 1;
    }
}