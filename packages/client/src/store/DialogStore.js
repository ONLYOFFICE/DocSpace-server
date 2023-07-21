import { makeAutoObservable } from "mobx";

class DialogStore {
  changeName = false;
  changeEmail = false;
  changePassword = false;
  changeOwner = false;
  deleteSelfProfile = false;
  deleteProfileEver = false;
  data = {};

  changeUserTypeDialogVisible = false;

  changeUserStatusDialogVisible = false;
  disableDialogVisible = false;
  sendInviteDialogVisible = false;
  resetAuthDialogVisible = false;
  dataReassignmentDialogVisible = false;
  dataReassignmentDeleteProfile = false;
  reassignDataIds = null;
  dataReassignmentDeleteAdministrator = null;

  constructor() {
    makeAutoObservable(this);
  }

  setChangeNameDialogVisible = (visible) => {
    this.changeName = visible;
  };

  setChangeEmailDialogVisible = (visible) => {
    this.changeEmail = visible;
  };

  setChangePasswordDialogVisible = (visible) => {
    this.changePassword = visible;
  };

  setChangeOwnerDialogVisible = (visible) => {
    this.changeOwner = visible;
  };

  setDeleteSelfProfileDialogVisible = (visible) => {
    this.deleteSelfProfile = visible;
  };

  setDeleteProfileDialogVisible = (visible) => {
    this.deleteProfileEver = visible;
  };

  setDataReassignmentDeleteProfile = (dataReassignmentDeleteProfile) => {
    this.dataReassignmentDeleteProfile = dataReassignmentDeleteProfile;
  };

  setDataReassignmentDeleteAdministrator = (
    dataReassignmentDeleteAdministrator
  ) => {
    this.dataReassignmentDeleteAdministrator =
      dataReassignmentDeleteAdministrator;
  };

  setDialogData = (data) => {
    this.data = data;
  };

  setReassignDataIds = (reassignDataIds) => {
    this.reassignDataIds = reassignDataIds;
  };

  setChangeUserTypeDialogVisible = (visible) => {
    this.changeUserTypeDialogVisible = visible;
  };

  setChangeUserStatusDialogVisible = (visible) => {
    this.changeUserStatusDialogVisible = visible;
  };

  setSendInviteDialogVisible = (visible) => {
    this.sendInviteDialogVisible = visible;
  };

  setResetAuthDialogVisible = (visible) => {
    this.resetAuthDialogVisible = visible;
  };

  setDataReassignmentDialogVisible = (visible) => {
    this.dataReassignmentDialogVisible = visible;
  };

  closeDialogs = () => {
    this.setChangeEmailDialogVisible(false);
    this.setChangePasswordDialogVisible(false);
    this.setChangeOwnerDialogVisible(false);
    this.setDeleteSelfProfileDialogVisible(false);
    this.setDeleteProfileDialogVisible(false);
    this.setDialogData({});

    this.setChangeUserTypeDialogVisible(false);
    this.setChangeUserStatusDialogVisible(false);

    this.setSendInviteDialogVisible(false);
    this.setResetAuthDialogVisible(false);
  };
}

export default DialogStore;
