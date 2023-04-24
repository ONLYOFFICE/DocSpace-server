import React, { useState, useEffect, useRef, memo, useCallback } from "react";
import { inject, observer } from "mobx-react";
import { FixedSizeList as List } from "react-window";
import CustomScrollbarsVirtualList from "@docspace/components/scrollbar/custom-scrollbars-virtual-list";
import useResizeObserver from "use-resize-observer";
import Item from "./Item";

import { StyledRow, ScrollList } from "../StyledInvitePanel";

const FOOTER_HEIGHT = 70;
const USER_ITEM_HEIGHT = 48;

const Row = memo(({ data, index, style }) => {
  const {
    inviteItems,
    setInviteItems,
    changeInviteItem,
    t,
    setHasErrors,
    roomType,
    isOwner,
    inputsRef,
  } = data;

  if (inviteItems === undefined) return;

  const item = inviteItems[index];

  return (
    <StyledRow key={item.id} style={style}>
      <Item
        t={t}
        item={item}
        setInviteItems={setInviteItems}
        changeInviteItem={changeInviteItem}
        inviteItems={inviteItems}
        setHasErrors={setHasErrors}
        roomType={roomType}
        isOwner={isOwner}
        inputsRef={inputsRef}
      />
    </StyledRow>
  );
});

const ItemsList = ({
  t,
  setInviteItems,
  inviteItems,
  changeInviteItem,
  setHasErrors,
  roomType,
  isOwner,
  externalLinksVisible,
  scrollAllPanelContent,
  inputsRef,
}) => {
  const [bodyHeight, setBodyHeight] = useState(0);
  const [offsetTop, setOffsetTop] = useState(0);
  const bodyRef = useRef();
  const { height } = useResizeObserver({ ref: bodyRef });

  const onBodyResize = useCallback(() => {
    const heightList = height ? height : bodyRef.current.offsetHeight;

    const calculatedHeight = scrollAllPanelContent
      ? inviteItems.length * USER_ITEM_HEIGHT
      : heightList - FOOTER_HEIGHT;

    setBodyHeight(calculatedHeight);
    setOffsetTop(bodyRef.current.offsetTop);
  }, [
    height,
    bodyRef?.current?.offsetHeight,
    inviteItems.length,
    scrollAllPanelContent,
  ]);

  useEffect(() => {
    onBodyResize();
  }, [
    bodyRef.current,
    externalLinksVisible,
    height,
    inviteItems.length,
    scrollAllPanelContent,
  ]);

  let itemCount = inviteItems.length;

  //Scroll blinking fix
  if (scrollAllPanelContent) {
    itemCount =
      bodyHeight / inviteItems.length != USER_ITEM_HEIGHT
        ? inviteItems.length - 1
        : inviteItems.length;
  }

  return (
    <ScrollList
      offsetTop={offsetTop}
      ref={bodyRef}
      scrollAllPanelContent={scrollAllPanelContent}
    >
      <List
        height={bodyHeight}
        width="auto"
        itemCount={itemCount}
        itemSize={USER_ITEM_HEIGHT}
        itemData={{
          inviteItems,
          setInviteItems,
          changeInviteItem,
          setHasErrors,
          roomType,
          isOwner,
          inputsRef,
          t,
        }}
        outerElementType={CustomScrollbarsVirtualList}
      >
        {Row}
      </List>
    </ScrollList>
  );
};

export default inject(({ auth, dialogsStore }) => {
  const { setInviteItems, inviteItems, changeInviteItem } = dialogsStore;
  const { isOwner } = auth.userStore.user;

  return {
    setInviteItems,
    inviteItems,
    changeInviteItem,
    isOwner,
  };
})(observer(ItemsList));
