import { create } from 'zustand';
import { persist } from 'zustand/middleware';

interface AuthState {
  isAuthenticated: boolean;
  token: string | null;
  refreshToken: string | null;
  accountId: number | null;
  accountName: string | null;
  phoneNum: string | null;
  email: string | null;
  login: (params: {
    token: string;
    refreshToken: string;
    accountId: number;
    accountName: string;
    phoneNum?: string | null;
    email?: string | null;
  }) => void;
  logout: () => void;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set) => ({
      isAuthenticated: false,
      token: null,
      refreshToken: null,
      accountId: null,
      accountName: null,
      phoneNum: null,
      email: null,
      login: ({ token, refreshToken, accountId, accountName, phoneNum, email }) => {
        localStorage.setItem('token', token);
        set({
          isAuthenticated: true,
          token,
          refreshToken,
          accountId,
          accountName,
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
          accountId: null,
          accountName: null,
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
