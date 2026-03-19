import { create } from "zustand";
import type { AuthUser } from "../types/auth";
import { authService } from "../services/authService";
import {
  clearSessionActivity,
  hasSessionExpired,
  recordSessionActivity
} from "../utils/sessionTimeout";

type AuthState = {
  user: AuthUser | null;
  isReturningUser: boolean;
  isAuthResolved: boolean;
  setUser: (user: AuthUser) => void;
  logout: () => Promise<void>;
  loadUser: () => Promise<void>;
};

const getSeenUserKey = (email: string) => `pft_seen_user_${email.trim().toLowerCase()}`;

export const useAuthStore = create<AuthState>((set) => ({
  user: null,
  isReturningUser: false,
  isAuthResolved: false,

  setUser: (user) => {
    const seenUserKey = getSeenUserKey(user.email);
    const isReturningUser = localStorage.getItem(seenUserKey) === "true";

    localStorage.setItem(seenUserKey, "true");
    recordSessionActivity();
    set({ user, isReturningUser, isAuthResolved: true });
  },

  logout: async () => {
    try {
      await authService.logout();
    } catch {
      // Best effort logout; still clear local session state.
    }

    clearSessionActivity();
    set({ user: null, isReturningUser: false, isAuthResolved: true });
  },

  loadUser: async () => {
    if (hasSessionExpired()) {
      try {
        await authService.logout();
      } catch {
        // Ignore cookie cleanup failures during startup.
      }

      clearSessionActivity();
      set({ user: null, isReturningUser: false, isAuthResolved: true });
      return;
    }

    try {
      const user = await authService.me();
      set({
        user,
        isReturningUser: localStorage.getItem(getSeenUserKey(user.email)) === "true",
        isAuthResolved: true
      });
    } catch {
      clearSessionActivity();
      set({ user: null, isReturningUser: false, isAuthResolved: true });
    }
  }
}));
