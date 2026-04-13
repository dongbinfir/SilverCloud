import { message } from 'antd';
import {
  getIdentityWebAPIV1,
  type LoginCommand,
  type RefreshTokenCommand,
  type LogoutCommand,
} from '@identity-api';

const api = getIdentityWebAPIV1();

export async function Login(params: LoginCommand) {
  try {
    const result = await api.postIdentityAuthorizationsLogin(params);
    return result;
  } catch (error) {
    message.error('Login failed.');
    throw error;
  }
}

export async function RefreshToken(params: RefreshTokenCommand) {
  try {
    const result = await api.postIdentityAuthorizationsRefreshToken(params);
    return result;
  } catch (error) {
    message.error('Refresh token failed.');
    throw error;
  }
}

export async function Logout(params: LogoutCommand) {
  try {
    await api.postIdentityAuthorizationsLogout(params);
    message.success('Logout successfully.');
  } catch (error) {
    message.error('Logout failed.');
    throw error;
  }
}
