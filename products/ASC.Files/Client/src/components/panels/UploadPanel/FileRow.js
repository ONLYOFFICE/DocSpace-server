import React from 'react';
import styled, { css } from 'styled-components';
import Tooltip from '@appserver/components/tooltip';
import Row from '@appserver/components/row';
import Text from '@appserver/components/text';
import Link from '@appserver/components/link';
import LoadingButton from './LoadingButton';
import ShareButton from './ShareButton';
import LoadErrorIcon from '../../../../public/images/load.error.react.svg';
import IconButton from '@appserver/components/icon-button';
import { inject, observer } from 'mobx-react';
import { withTranslation } from 'react-i18next';

const StyledFileRow = styled(Row)`
  padding: 0 16px;
  box-sizing: border-box;
  font-weight: 600;

  .row_context-menu-wrapper {
    width: auto;
  }

  .row_content > a,
  .row_content > p {
    margin: auto 0;
    line-height: 16px;
  }

  .upload_panel-icon {
    margin-left: auto;
    padding-left: 16px;
    line-height: 24px;
    display: flex;
    align-items: center;
    flex-direction: row-reverse;

    svg {
      width: 16px;
      height: 16px;
    }
  }

  .img_error {
    filter: grayscale(1);
  }

  .convert_icon {
    padding-right: 12px;
  }

  .upload-panel_file-row-link {
    ${(props) =>
      !props.isMediaActive &&
      css`
        cursor: default;
      `}
  }
`;

const FileRow = (props) => {
  const {
    t,
    theme,
    item,
    uploaded,
    cancelCurrentUpload,
    cancelCurrentFileConversion,
    //onMediaClick,
    currentFileUploadProgress,
    fileIcon,
    isMedia,
    ext,
    name,
    isPersonal,
    setMediaViewerData,
    setUploadPanelVisible,
    isMediaActive,
    downloadInCurrentTab,
  } = props;

  const onCancelCurrentUpload = (e) => {
    //console.log("cancel upload ", e);
    const { id, action, fileId } = e.currentTarget.dataset;

    return action === 'convert' ? cancelCurrentFileConversion(fileId) : cancelCurrentUpload(id);
  };

  const onMediaClick = (id) => {
    if (!isMediaActive) return;
    const item = { visible: true, id: id };
    setMediaViewerData(item);
    setUploadPanelVisible(false);
  };

  const onCancelClick = !item.inConversion ? { onClick: onCancelCurrentUpload } : {};

  return (
    <>
      <StyledFileRow
        className="download-row"
        key={item.uniqueId}
        checkbox={false}
        element={<img className={item.error && 'img_error'} src={fileIcon} alt="" />}
        isMediaActive={isMediaActive}>
        <>
          {item.fileId ? (
            isMedia ? (
              <Link
                className="upload-panel_file-row-link"
                fontWeight="600"
                color={item.error || !isMediaActive ? theme.filesPanels.upload.color : ''}
                truncate
                onClick={() => onMediaClick(item.fileId)}>
                {name}
              </Link>
            ) : (
              <Link
                fontWeight="600"
                color={item.error && theme.filesPanels.upload.color}
                truncate
                href={item.fileInfo ? item.fileInfo.webUrl : ''}
                target={downloadInCurrentTab ? '_self' : '_blank'}>
                {name}
              </Link>
            )
          ) : (
            <Text fontWeight="600" color={item.error && theme.filesPanels.upload.color} truncate>
              {name}
            </Text>
          )}
          {ext ? (
            <Text fontWeight="600" color={theme.filesPanels.upload.color}>
              {ext}
            </Text>
          ) : (
            <></>
          )}
          {item.fileId && !item.error ? (
            <>
              {item.action === 'upload' && !isPersonal && <ShareButton uniqueId={item.uniqueId} />}
              {item.action === 'convert' && (
                <div
                  className="upload_panel-icon"
                  data-id={item.uniqueId}
                  data-file-id={item.fileId}
                  data-action={item.action}
                  {...onCancelClick}>
                  <LoadingButton
                    isConversion
                    inConversion={item.inConversion}
                    percent={item.convertProgress}
                  />
                  <IconButton
                    iconName="/static/images/refresh.react.svg"
                    className="convert_icon"
                    size="medium"
                    isfill={true}
                    // color={theme.filesPanels.upload.color}
                  />
                </div>
              )}
            </>
          ) : item.error || (!item.fileId && uploaded) ? (
            <div className="upload_panel-icon">
              <LoadErrorIcon
                size="medium"
                data-for="errorTooltip"
                data-tip={item.error || t('Common:UnknownError')}
              />
              <Tooltip
                id="errorTooltip"
                offsetTop={0}
                getContent={(dataTip) => <Text fontSize="13px">{dataTip}</Text>}
                effect="float"
                place="left"
                maxWidth="250px"
                color={theme.filesPanels.upload.tooltipColor}
              />
            </div>
          ) : (
            <div
              className="upload_panel-icon"
              data-id={item.uniqueId}
              onClick={onCancelCurrentUpload}>
              <LoadingButton percent={currentFileUploadProgress} />
            </div>
          )}
        </>
      </StyledFileRow>
    </>
  );
};

export default inject(({ auth, formatsStore, uploadDataStore, mediaViewerDataStore }, { item }) => {
  let ext;
  let name;
  let splitted;
  if (item.file) {
    splitted = item.file.name.split('.');
    ext = splitted.length > 1 ? '.' + splitted.pop() : '';
    name = splitted[0];
  } else {
    ext = item.fileInfo.fileExst;
    splitted = item.fileInfo.title.split('.');
    name = splitted[0];
  }

  const { personal, theme } = auth.settingsStore;
  const { iconFormatsStore, mediaViewersFormatsStore, docserviceStore } = formatsStore;
  const { canViewedDocs } = docserviceStore;
  const {
    uploaded,
    primaryProgressDataStore,
    cancelCurrentUpload,
    cancelCurrentFileConversion,
    setUploadPanelVisible,
  } = uploadDataStore;
  const { playlist, setMediaViewerData } = mediaViewerDataStore;
  const { loadingFile: file } = primaryProgressDataStore;
  const isMedia = mediaViewersFormatsStore.isMediaOrImage(ext);
  const isMediaActive = playlist.findIndex((el) => el.fileId === item.fileId) !== -1;

  const fileIcon = iconFormatsStore.getIconSrc(ext, 24);

  const loadingFile = !file || !file.uniqueId ? null : file;

  const currentFileUploadProgress =
    file && loadingFile.uniqueId === item.uniqueId ? loadingFile.percent : null;

  const { isArchive } = iconFormatsStore;

  const downloadInCurrentTab = isArchive(ext) || !canViewedDocs(ext);

  return {
    isPersonal: personal,
    theme,
    currentFileUploadProgress,
    uploaded,
    isMedia,
    fileIcon,
    ext,
    name,
    loadingFile,
    isMediaActive,
    downloadInCurrentTab,

    cancelCurrentUpload,
    cancelCurrentFileConversion,
    setMediaViewerData,
    setUploadPanelVisible,
  };
})(withTranslation('UploadPanel')(observer(FileRow)));
