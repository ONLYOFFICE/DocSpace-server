// (c) Copyright Ascensio System SIA 2009-2024
// 
// This program is a free software product.
// You can redistribute it and/or modify it under the terms
// of the GNU Affero General Public License (AGPL) version 3 as published by the Free Software
// Foundation. In accordance with Section 7(a) of the GNU AGPL its Section 15 shall be amended
// to the effect that Ascensio System SIA expressly excludes the warranty of non-infringement of
// any third-party rights.
// 
// This program is distributed WITHOUT ANY WARRANTY, without even the implied warranty
// of MERCHANTABILITY or FITNESS FOR A PARTICULAR  PURPOSE. For details, see
// the GNU AGPL at: http://www.gnu.org/licenses/agpl-3.0.html
// 
// You can contact Ascensio System SIA at Lubanas st. 125a-25, Riga, Latvia, EU, LV-1021.
// 
// The  interactive user interfaces in modified source and object code versions of the Program must
// display Appropriate Legal Notices, as required under Section 5 of the GNU AGPL version 3.
// 
// Pursuant to Section 7(b) of the License you must retain the original Product logo when
// distributing the program. Pursuant to Section 7(e) we decline to grant you any rights under
// trademark law for use of our trademarks.
// 
// All the Product's GUI elements, including illustrations and icon sets, as well as technical writing
// content are licensed under the terms of the Creative Commons Attribution-ShareAlike 4.0
// International. See the License terms at http://creativecommons.org/licenses/by-sa/4.0/legalcode

// Options for the phantom script
exports.phantom = {
  windowSize: {
    width: 1024
  , height: 768
  }
, shotSize: {
    width: 'window'
  , height: 'window'
  }
, shotOffset: {
    left: 0
  , right: 0
  , top: 0
  , bottom: 0
  }
, defaultWhiteBackground: false
, customCSS: ''
, takeShotOnCallback: false
, streamType: 'png'
, siteType: 'url'
, renderDelay: 0
, quality: 75
, errorIfStatusIsNot200: false
, errorIfJSException: false
, cookies: []
, captureSelector: false
, zoomFactor: 1
};

// Options that are just passed to the phantom page object
exports.phantomPage = ['paperSize', 'customHeaders', 'settings', 'zoomFactor'];

// Options that are callbacks for various phantom events
exports.phantomCallback = ['onAlert', 'onCallback', 'onClosing', 'onConfirm',
  'onConsoleMessage', 'onError', 'onFilePicker', 'onInitialized',
  'onLoadFinished', 'onLoadStarted', 'onNavigationRequested', 'onPageCreated',
  'onPrompt', 'onResourceRequested', 'onResourceReceived',
  'onResourceTimeout', 'onResourceError', 'onUrlChanged'];

// Options that are used in the calling node script
exports.caller = {
  phantomPath: 'phantomjs'
, phantomConfig: ''
, timeout: 0
};


/*
 * Merge the two objects, using the value from `a` when the objects conflict
 *
 * @param (Object) a
 * @param (Object) b
 * @return (Object)
 */
exports.mergeObjects = function mergeObjects(a, b) {
  var merged = {};

  Object.keys(a).forEach(function(key) {
    merged[key] = toString.call(a[key]) === '[object Object]'
      ? mergeObjects(a[key], b[key] || {})
      : a[key] || b[key];
  });

  Object.keys(b).forEach(function(key) {
    if (merged.hasOwnProperty(key)) return;
    merged[key] = b[key];
  });

  return merged;
};


/*
 * Filter the object `obj` to contain only the given keys
 *
 * @param (Object) obj
 * @param (Array) keys
 * @return (Object)
 */
exports.filterObject = function filterObject(obj, keys) {
  var filtered = {};

  keys.forEach(function(key) {
    if (obj[key]) filtered[key] = obj[key];
  });

  return filtered;
};
