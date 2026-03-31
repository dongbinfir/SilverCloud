import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  isAuthenticated: boolean;
  token: string | null;
  refreshToken: string | null;
  userId: string | null;
  userName: string | null;
  phoneNum: string | null;
  email: string | null;
  login: (params: {
    token: string;
    refreshToken: string;
    userId: string;
    userName: string;
    phoneNum?: string;
    email?: string;
  }) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      userId: null,
      userName: null,
      phoneNum: null,
      email: null,
      login: ({ token, refreshToken, userId, userName, phoneNum, email }) => {
        localStorage.setItem('token', token);
        set({
          isAuthenticated: true,
          token,
          refreshToken,
          userId,
          userName,
          phoneNum: phoneNum ?? null,
          email: email ?? null,
        });
      },
      logout: () => {
        localStorage.removeItem('token');
        set({
          isAuthenticated: false,
          token: null,
          refreshToken: null,
          userId: null,
          userName: null,
          phoneNum: null,
          email: null,
        });
      },
    }),
    {
      name: 'auth-storage',
    },
  ),
);
