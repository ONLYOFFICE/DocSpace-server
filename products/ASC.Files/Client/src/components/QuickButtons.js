import React from "react";
import styled from "styled-components";
import IconButton from "@appserver/components/icon-button";
import commonIconsStyles from "@appserver/components/utils/common-icons-style";
import { isMobile, isTablet } from "react-device-detect";

export const StyledIcon = styled(IconButton)`
  ${commonIconsStyles}
`;

const QuickButtons = ({
  item,
  theme,
  sectionWidth,
  isTrashFolder,
  accessToEdit,
  showShare,
  onClickLock,
  onClickFavorite,
  onClickShare,
  viewAs,
}) => {
  const { id, locked, fileStatus, title, fileExst, shared } = item;

  const isFavorite = fileStatus === 32;
  const isNewWithFav = fileStatus === 34;
  const isEditingWithFav = fileStatus === 33;
  const showFavorite = isFavorite || isNewWithFav || isEditingWithFav;

  const isTile = viewAs === "tile";

  const iconShare = shared
    ? "/static/images/file.actions.share.react.svg"
    : "/static/images/catalog.share.react.svg";

  const colorShare = shared
    ? theme.filesQuickButtons.sharedColor
    : theme.filesQuickButtons.color;

  const iconLock = locked
    ? "/static/images/file.actions.locked.react.svg"
    : "/static/images/locked.react.svg";

  const colorLock = locked
    ? theme.filesQuickButtons.sharedColor
    : theme.filesQuickButtons.color;

  const iconFavorite = showFavorite
    ? "/static/images/file.actions.favorite.react.svg"
    : "/static/images/favorite.react.svg";

  const colorFavorite = showFavorite
    ? theme.filesQuickButtons.sharedColor
    : theme.filesQuickButtons.color;

  const tabletViewQuickButton =
    !isTile && ((sectionWidth > 500 && sectionWidth <= 1024) || isTablet);
  const sizeQuickButton = isTile || tabletViewQuickButton ? "medium" : "small";

  const displayShare = viewAs === "row" && (isMobile || sectionWidth <= 500);
  const displayLock = !locked && (isMobile || sectionWidth <= 500);
  const displayFavorite = !showFavorite && (isMobile || sectionWidth <= 500);

  return (
    <div className="badges additional-badges">
      {item.canShare && showShare && (!displayShare || isTile) && (
        <StyledIcon
          iconName={iconShare}
          className="badge share-button-icon"
          size={sizeQuickButton}
          onClick={onClickShare}
          color={colorShare}
          hoverColor={theme.filesQuickButtons.sharedColor}
        />
      )}
      {fileExst &&
        accessToEdit &&
        !isTrashFolder &&
        (!displayLock || isTile) && (
          <StyledIcon
            iconName={iconLock}
            className="badge lock-file icons-group"
            size={sizeQuickButton}
            data-id={id}
            data-locked={locked ? true : false}
            onClick={onClickLock}
            color={colorLock}
            hoverColor={theme.filesQuickButtons.sharedColor}
          />
        )}
      {fileExst && !isTrashFolder && (!displayFavorite || isTile) && (
        <StyledIcon
          iconName={iconFavorite}
          className="favorite badge icons-group"
          size={sizeQuickButton}
          data-id={id}
          data-title={title}
          onClick={() => onClickFavorite(showFavorite)}
          color={colorFavorite}
          hoverColor={theme.filesQuickButtons.hoverColor}
        />
      )}
    </div>
  );
};

export default QuickButtons;
