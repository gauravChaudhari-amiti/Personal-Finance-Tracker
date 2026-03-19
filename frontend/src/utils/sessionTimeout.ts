const SESSION_ACTIVITY_KEY = "pft_last_activity";
const SESSION_NOTICE_KEY = "pft_auth_notice";

export const SESSION_TIMEOUT_MS = 60 * 60 * 1000;
export const SESSION_EXPIRED_NOTICE = "You were signed out after 1 hour of inactivity.";

export const recordSessionActivity = (timestamp = Date.now()) => {
  localStorage.setItem(SESSION_ACTIVITY_KEY, timestamp.toString());
};

export const clearSessionActivity = () => {
  localStorage.removeItem(SESSION_ACTIVITY_KEY);
};

export const getLastSessionActivity = () => {
  const raw = localStorage.getItem(SESSION_ACTIVITY_KEY);
  if (!raw) return null;

  const value = Number(raw);
  return Number.isFinite(value) ? value : null;
};

export const hasSessionExpired = (now = Date.now()) => {
  const lastActivity = getLastSessionActivity();
  if (!lastActivity) return false;

  return now - lastActivity >= SESSION_TIMEOUT_MS;
};

export const setSessionExpiredNotice = () => {
  localStorage.setItem(SESSION_NOTICE_KEY, SESSION_EXPIRED_NOTICE);
};

export const consumeSessionNotice = () => {
  const notice = localStorage.getItem(SESSION_NOTICE_KEY);
  if (!notice) return "";

  localStorage.removeItem(SESSION_NOTICE_KEY);
  return notice;
};
