import { create } from "zustand";
import type { AuthUser } from "../types/auth";
import {
  clearSessionActivity,
  hasSessionExpired,
  recordSessionActivity
} from "../utils/sessionTimeout";

type AuthState = {
  user: AuthUser | null;
  setUser: (user: AuthUser) => void;
  logout: () => void;
  loadUser: () => void;
};

export const useAuthStore = create<AuthState>((set) => ({
  user: null,

  setUser: (user) => {
    localStorage.setItem("pft_user", JSON.stringify(user));
    recordSessionActivity();
    set({ user });
  },

  logout: () => {
    localStorage.removeItem("pft_user");
    clearSessionActivity();
    set({ user: null });
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
    set({ user });
  }
}));
