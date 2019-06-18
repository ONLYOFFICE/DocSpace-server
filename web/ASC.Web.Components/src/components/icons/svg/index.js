import createStyledIcon from './create-styled-icon';
import OrigPeopleIcon from './people.react.svg';
import OrigCalendarIcon from './calendar.react.svg';
import OrigExpanderDownIcon from './expander-down.react.svg';
import OrigExpanderRightIcon from './expander-right.react.svg';

import OrigGuestIcon from './guest.react.svg';
import OrigAdministratorIcon from './administrator.react.svg';
import OrigOwnerIcon from './owner.react.svg';


export const PeopleIcon = createStyledIcon(
  OrigPeopleIcon,
  'PeopleIcon'
);
export const CalendarIcon = createStyledIcon(
  OrigCalendarIcon,
  'CalendarIcon'
);
export const ExpanderDownIcon = createStyledIcon(
  OrigExpanderDownIcon,
  'ExpanderDownIcon'
);
export const ExpanderRightIcon = createStyledIcon(
  OrigExpanderRightIcon,
  'ExpanderRight'
);

export const GuestIcon = createStyledIcon(
  OrigGuestIcon,
  'GuestIcon'
);
export const AdministratorIcon = createStyledIcon(
  OrigAdministratorIcon,
  'AdministratorIcon'
);
export const OwnerIcon = createStyledIcon(
  OrigOwnerIcon,
  'OwnerIcon'
);