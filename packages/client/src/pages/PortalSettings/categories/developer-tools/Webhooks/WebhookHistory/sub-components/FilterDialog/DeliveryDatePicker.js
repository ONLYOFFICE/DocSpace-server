import React, { useState, useEffect, useRef } from "react";
import moment from "moment";
import { inject, observer } from "mobx-react";

import styled, { css } from "styled-components";

import { Text } from "@docspace/components";
import { SelectorAddButton } from "@docspace/components";
import { SelectedItem } from "@docspace/components";

import { Calendar } from "@docspace/components";
import { TimePicker } from "@docspace/components";
import { isMobileOnly } from "react-device-detect";

const TimePickerCell = styled.span`
  margin-left: 8px;
  display: inline-flex;
  align-items: center;

  .timePickerItem {
    display: inline-flex;
    align-items: center;
    margin-right: 16px;
  }
`;

const StyledCalendar = styled(Calendar)`
  position: absolute;
  ${(props) =>
    props.isMobile &&
    css`
      position: fixed;
      bottom: 0;
      left: 0;
    `}
`;

const DeliveryDatePicker = ({ Selectors, filters, setFilters, isApplied, setIsApplied }) => {
  const [isCalendarOpen, setIsCalendarOpen] = useState(false);
  const [isTimeOpen, setIsTimeOpen] = useState(false);

  const calendarRef = useRef();
  const selectorRef = useRef();

  const setDeliveryDate = (date) => {
    setFilters((prevFilters) => ({ ...prevFilters, deliveryDate: date }));
  };
  const setDeliveryFrom = (date) => {
    setFilters((prevFilters) => ({ ...prevFilters, deliveryFrom: date }));
  };
  const setDeliveryTo = (date) => {
    setFilters((prevFilters) => ({ ...prevFilters, deliveryTo: date }));
  };

  const toggleCalendar = () => setIsCalendarOpen((prevIsCalendarOpen) => !prevIsCalendarOpen);
  const closeCalendar = () => {
    setIsApplied(false);
    setIsCalendarOpen(false);
  };

  const showTimePicker = () => setIsTimeOpen(true);

  const deleteSelectedDate = (e) => {
    e.stopPropagation();
    setFilters((prevFilters) => ({
      deliveryDate: null,
      deliveryFrom: moment().startOf("day"),
      deliveryTo: moment().endOf("day"),
      status: prevFilters.status,
    }));
    setIsTimeOpen(false);
    setIsApplied(false);
  };

  const handleClick = (e) => {
    !selectorRef?.current?.contains(e.target) &&
      !calendarRef?.current?.contains(e.target) &&
      setIsCalendarOpen(false);
  };

  useEffect(() => {
    document.addEventListener("click", handleClick, { capture: true });
    return () => document.removeEventListener("click", handleClick, { capture: true });
  }, []);

  const CalendarElement = () => (
    <StyledCalendar
      selectedDate={filters.deliveryDate}
      setSelectedDate={setDeliveryDate}
      onChange={closeCalendar}
      isMobile={isMobileOnly}
      forwardedRef={calendarRef}
    />
  );

  const DateSelector = () => (
    <div>
      <SelectorAddButton title="add" onClick={toggleCalendar} style={{ marginRight: "8px" }} />
      <Text isInline fontWeight={600} color="#A3A9AE">
        Select date
      </Text>
      {isCalendarOpen && <CalendarElement />}
    </div>
  );

  const SelectedDate = () => (
    <SelectedItem
      onClose={deleteSelectedDate}
      text={moment(filters.deliveryDate).format("DD MMM YYYY")}
    />
  );

  const SelectedDateWithCalendar = () => (
    <div>
      <SelectedItem
        onClose={deleteSelectedDate}
        text={moment(filters.deliveryDate).format("DD MMM YYYY")}
        onClick={toggleCalendar}
      />
      {isCalendarOpen && <CalendarElement />}
    </div>
  );

  const SelectedDateTime = () => (
    <div>
      <SelectedItem
        onClose={deleteSelectedDate}
        text={
          moment(filters.deliveryDate).format("DD MMM YYYY") +
          " " +
          moment(filters.deliveryFrom).format("HH:mm") +
          " - " +
          moment(filters.deliveryTo).format("HH:mm")
        }
        onClick={toggleCalendar}
      />
      {isCalendarOpen && <CalendarElement />}
    </div>
  );

  const TimeSelectorAdder = () => (
    <TimePickerCell>
      <SelectorAddButton title="add" onClick={showTimePicker} style={{ marginRight: "8px" }} />
      <Text isInline fontWeight={600} color="#A3A9AE">
        Select Delivery time
      </Text>
    </TimePickerCell>
  );

  const isEqualDates = (firstDate, secondDate) => {
    return firstDate.format() === secondDate.format();
  };

  const isTimeEqual =
    isEqualDates(filters.deliveryFrom, filters.deliveryFrom.clone().startOf("day")) &&
    isEqualDates(filters.deliveryTo, filters.deliveryTo.clone().endOf("day"));

  const isTimeValid = filters.deliveryTo > filters.deliveryFrom;

  return (
    <>
      <Text fontWeight={600} fontSize="15px">
        Delivery date
      </Text>
      <Selectors ref={selectorRef}>
        {filters.deliveryDate === null ? (
          <DateSelector />
        ) : isApplied ? (
          isTimeEqual ? (
            <SelectedDateWithCalendar />
          ) : (
            <SelectedDateTime />
          )
        ) : (
          <SelectedDate />
        )}
        {filters.deliveryDate !== null &&
          !isApplied &&
          (isTimeOpen ? (
            <TimePickerCell>
              <span className="timePickerItem">
                <Text isInline fontWeight={600} color="#A3A9AE" style={{ marginRight: "8px" }}>
                  From
                </Text>
                <TimePicker
                  date={filters.deliveryFrom}
                  setDate={setDeliveryFrom}
                  hasError={!isTimeValid}
                  tabIndex={1}
                />
              </span>
              <Text isInline fontWeight={600} color="#A3A9AE" style={{ marginRight: "8px" }}>
                Before
              </Text>
              <TimePicker
                date={filters.deliveryTo}
                setDate={setDeliveryTo}
                hasError={!isTimeValid}
                tabIndex={2}
              />
            </TimePickerCell>
          ) : (
            <TimeSelectorAdder />
          ))}
      </Selectors>
    </>
  );
};

export default inject(({ webhooksStore }) => {
  const {} = webhooksStore;

  return {};
})(observer(DeliveryDatePicker));
