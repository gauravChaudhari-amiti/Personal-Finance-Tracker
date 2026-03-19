import { create } from "zustand";
import type { AuthUser } from "../types/auth";
import {
  clearSessionActivity,
  hasSessionExpired,
  recordSessionActivity
} from "../utils/sessionTimeout";

type AuthState = {
  user: AuthUser | null;
  isReturningUser: boolean;
  setUser: (user: AuthUser) => void;
  logout: () => void;
  loadUser: () => void;
};

const getSeenUserKey = (email: string) => `pft_seen_user_${email.trim().toLowerCase()}`;

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isReturningUser: false,

  setUser: (user) => {
    const seenUserKey = getSeenUserKey(user.email);
    const isReturningUser = localStorage.getItem(seenUserKey) === "true";

    localStorage.setItem("pft_user", JSON.stringify(user));
    localStorage.setItem(seenUserKey, "true");
    recordSessionActivity();
    set({ user, isReturningUser });
  },

  logout: () => {
    localStorage.removeItem("pft_user");
    clearSessionActivity();
    set({ user: null, isReturningUser: false });
  },

  loadUser: () => {
    const raw = localStorage.getItem("pft_user");
    if (!raw) return;

    if (hasSessionExpired()) {
      localStorage.removeItem("pft_user");
      clearSessionActivity();
      return;
    }

    const user = JSON.parse(raw) as AuthUser;
    set({
      user,
      isReturningUser: localStorage.getItem(getSeenUserKey(user.email)) === "true"
    });
  }
}));
