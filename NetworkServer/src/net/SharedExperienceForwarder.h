#pragma once
#include <cstdint>
#include <vector>
namespace tdr::net { class TcpClientSession; struct ForwardedSharedExperience final { std::uint32_t targetPlayerId=0U; std::vector<std::uint8_t> payload; }; class SharedExperienceForwarder final { public: static ForwardedSharedExperience Forward(const TcpClientSession&, const std::vector<std::uint8_t>&); }; }
