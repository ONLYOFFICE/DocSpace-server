import React from 'react';
import Badge from '@appserver/components/badge';
import IconButton from '@appserver/components/icon-button';
import {
  StyledFavoriteIcon,
  StyledFileActionsConvertEditDocIcon,
  StyledFileActionsEditFormIcon,
  StyledFileActionsLockedIcon,
} from './Icons';

const Badges = ({
  t,
  theme,
  newItems,
  item,
  canWebEdit,
  isTrashFolder,
  isPrivacyFolder,
  isDesktopClient,
  canConvert,
  accessToEdit,
  showNew,
  onFilesClick,
  onClickLock,
  onClickFavorite,
  onShowVersionHistory,
  onBadgeClick,
  setConvertDialogVisible,
}) => {
  const { id, locked, fileStatus, version, versionGroup, title, fileExst } = item;

  const isFavorite = fileStatus === 32;
  const isEditing = fileStatus === 1;
  const isNewWithFav = fileStatus === 34;
  const isEditingWithFav = fileStatus === 33;
  const showEditBadge = !locked || item.access === 0;
  const isPrivacy = isPrivacyFolder && isDesktopClient;
  const isForm = fileExst === '.oform';

  return fileExst ? (
    <div className="badges additional-badges">
      {canConvert && !isTrashFolder && (
        <IconButton
          onClick={setConvertDialogVisible}
          iconName="/static/images/refresh.react.svg"
          className="badge icons-group can-convert"
          size="small"
          isfill={true}
          color={theme.filesBadges.iconColor}
          hoverColor={theme.filesBadges.hoverIconColor}
        />
      )}
      {canWebEdit &&
        !isEditing &&
        !isEditingWithFav &&
        !isTrashFolder &&
        !isPrivacy &&
        accessToEdit &&
        showEditBadge &&
        !canConvert && (
          <IconButton
            onClick={onFilesClick}
            iconName={
              isForm
                ? '/static/images/access.edit.form.react.svg'
                : '/static/images/access.edit.react.svg'
            }
            className="badge icons-group edit"
            size="small"
            isfill={true}
            color={theme.filesBadges.iconColor}
            hoverColor={theme.filesBadges.hoverIconColor}
          />
        )}
      {(isEditing || isEditingWithFav) &&
        React.createElement(
          isForm ? StyledFileActionsEditFormIcon : StyledFileActionsConvertEditDocIcon,
          {
            onClick: onFilesClick,
            className: 'badge icons-group is-editing',
            size: 'small',
          },
        )}
      {locked && accessToEdit && !isTrashFolder && (
        <StyledFileActionsLockedIcon
          className="badge lock-file icons-group"
          size="small"
          data-id={id}
          data-locked={true}
          onClick={onClickLock}
        />
      )}
      {(isFavorite || isNewWithFav || isEditingWithFav) && !isTrashFolder && (
        <StyledFavoriteIcon
          className="favorite icons-group badge"
          size="small"
          data-action="remove"
          data-id={id}
          data-title={title}
          onClick={onClickFavorite}
        />
      )}
      {version > 1 && (
        <Badge
          className="badge-version icons-group"
          backgroundColor={theme.filesBadges.backgroundColor}
          borderRadius="11px"
          color={theme.filesBadges.color}
          fontSize="10px"
          fontWeight={800}
          label={`Ver.${versionGroup}`}
          maxWidth="50px"
          onClick={onShowVersionHistory}
          padding="0 5px"
          data-id={id}
        />
      )}
      {(showNew || isNewWithFav) && (
        <Badge
          className="badge-version icons-group"
          backgroundColor={theme.filesBadges.badgeBackgroundColor}
          borderRadius="11px"
          color={theme.filesBadges.badgeColor}
          fontSize="10px"
          fontWeight={800}
          label={t('New')}
          maxWidth="50px"
          onClick={onBadgeClick}
          padding="0 5px"
          data-id={id}
        />
      )}
    </div>
  ) : (
    showNew && (
      <Badge
        className="new-items"
        backgroundColor={theme.filesBadges.badgeBackgroundColor}
        borderRadius="11px"
        color={theme.filesBadges.badgeColor}
        fontSize="10px"
        fontWeight={800}
        label={newItems}
        maxWidth="50px"
        onClick={onBadgeClick}
        padding="0 5px"
        data-id={id}
      />
    )
  );
};

export default Badges;
