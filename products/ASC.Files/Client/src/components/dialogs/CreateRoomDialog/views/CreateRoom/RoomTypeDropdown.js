import React, { useState } from "react";
import styled from "styled-components";
import { roomTypes } from "../../data";
import RoomType from "../../sub-components/RoomType";

const StyledRoomTypeDropdown = styled.div`
  display: flex;
  flex-direction: column;
  gap: 4px;
  width: 100%;

  .dropdown-content-wrapper {
    max-width: 100%;
    position: relative;
    background: #ffffff;
    ${(props) => !props.isOpen && "display: none"};

    .dropdown-content {
      background: #ffffff;
      overflow: visible;
      z-index: 400;
      top: 0;
      left: 0;
      box-sizing: border-box;
      width: 100%;
      position: absolute;
      display: flex;
      flex-direction: column;
      padding: 6px 0;
      border: 1px solid #d0d5da;
      box-shadow: 0px 12px 40px rgba(4, 15, 27, 0.12);
      border-radius: 6px;
    }
  }
`;

const RoomTypeDropdown = ({ t, currentRoomType, setRoomType }) => {
  const [isOpen, setIsOpen] = useState(false);
  const toggleIsOpen = () => setIsOpen(!isOpen);

  const [currentRoom] = roomTypes.filter(
    (room) => room.type === currentRoomType
  );

  return (
    <StyledRoomTypeDropdown isOpen={isOpen}>
      <RoomType
        t={t}
        room={currentRoom}
        type="dropdownButton"
        isOpen={isOpen}
        onClick={toggleIsOpen}
      />
      <div className="dropdown-content-wrapper">
        <div className="dropdown-content">
          {roomTypes.map((room) => (
            <RoomType
              t={t}
              key={room.type}
              room={room}
              type="dropdownItem"
              onClick={() => {
                setRoomType(room.type);
                toggleIsOpen();
              }}
            />
          ))}
        </div>
      </div>
    </StyledRoomTypeDropdown>
  );
};

export default RoomTypeDropdown;
