import { message } from 'antd';
import {
  getWebAPIV1,
  type LoginRequestDto,
  type RefreshTokenRequestDto,
  type LogoutRequestDto,
} from '@user-api';

const api = getWebAPIV1();

export async function Login(params: LoginRequestDto) {
  try {
    const result = await api.postUserAuthsLogin(params);
    return result;
  } catch (error) {
    message.error('Login failed.');
    throw error;
  }
}

export async function RefreshToken(params: RefreshTokenRequestDto) {
  try {
    const result = await api.postUserAuthsRefreshToken(params);
    return result;
  } catch (error) {
    message.error('Refresh token failed.');
    throw error;
  }
}

export async function Logout(params: LogoutRequestDto) {
  try {
    await api.postUserAuthsLogout(params);
    message.success('Logout successfully.');
  } catch (error) {
    message.error('Logout failed.');
    throw error;
  }
}
