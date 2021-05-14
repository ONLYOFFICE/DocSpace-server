import React from "react";
import ComboBox from "@appserver/components/combobox";
import { useTranslation } from "react-i18next";
import Checkbox from "@appserver/components/checkbox";
const ScheduleComponent = ({
  weeklySchedule,
  monthlySchedule,
  weekOptions,
  selectedOption,
  selectedWeekdayOption,
  selectedTimeOption,
  selectedMonthOption,
  timeOptionsArray,
  periodOptions,
  monthNumberOptionsArray,
  onSelectMonthNumberAndTimeOptions,
  selectedMaxCopies,
  onSelectMaxCopies,
  maxNumberCopiesArray,
  onSelectPeriod,
  onSelectWeedDay,
  isLoadingData,
}) => {
  const { t } = useTranslation("Settings");
  return (
    <div>
      <ComboBox
        options={periodOptions}
        selectedOption={{
          key: 0,
          label: selectedOption,
        }}
        onSelect={onSelectPeriod}
        isDisabled={isLoadingData}
        noBorder={false}
        scaled={false}
        scaledOptions={false}
        dropDownMaxHeight={300}
        size="content"
        className="backup_combobox "
      />
      {weeklySchedule && (
        <ComboBox
          options={weekOptions}
          selectedOption={{
            key: 0,
            label: selectedWeekdayOption,
          }}
          onSelect={onSelectWeedDay}
          isDisabled={isLoadingData}
          noBorder={false}
          scaled={false}
          scaledOptions={false}
          dropDownMaxHeight={300}
          size="content"
          className="backup_combobox"
        />
      )}
      {monthlySchedule && (
        <ComboBox
          options={monthNumberOptionsArray}
          selectedOption={{
            key: 0,
            label: selectedMonthOption,
          }}
          onSelect={onSelectMonthNumberAndTimeOptions}
          isDisabled={isLoadingData}
          noBorder={false}
          scaled={false}
          scaledOptions={false}
          dropDownMaxHeight={300}
          size="content"
          className="backup_combobox"
        />
      )}
      <ComboBox
        options={timeOptionsArray}
        selectedOption={{
          key: 0,
          label: selectedTimeOption,
        }}
        onSelect={onSelectMonthNumberAndTimeOptions}
        isDisabled={isLoadingData}
        noBorder={false}
        scaled={false}
        scaledOptions={false}
        dropDownMaxHeight={300}
        size="content"
        className="backup_combobox"
      />
      <ComboBox
        options={maxNumberCopiesArray}
        selectedOption={{
          key: 0,
          label: `${selectedMaxCopies} ${t("MaxCopies")}`,
        }}
        onSelect={onSelectMaxCopies}
        isDisabled={isLoadingData}
        noBorder={false}
        scaled={false}
        scaledOptions={false}
        dropDownMaxHeight={300}
        size="content"
        className="backup_combobox"
      />
      {/* <div className="backup-include_mail">
        <Checkbox
          name={"backupMail"}
          isChecked={backupMail}
          label={t("IncludeMail")}
          onChange={onClickCheckbox}
        />
      </div> */}
    </div>
  );
};

export default ScheduleComponent;
