import { message } from 'antd';
import {
  getIdentityWebAPIV1,
  type GetAccountInfoQuery,
  type CreateAccountInfoCommand,
  type UpdateAccountInfoCommand,
  type SearchAccountInfosQuery  ,
} from '@identity-api';

const api = getIdentityWebAPIV1();

export async function Get(params: GetAccountInfoQuery) {
  try {
    const result = await api.postIdentityAccountInfosGet(params);
    return result;
  } catch (error) {
    message.error('Failed to get user profile.');
    throw error;
  }
}

export async function Create(params: CreateAccountInfoCommand) {
  try {
    const result = await api.postIdentityAccountInfos(params);
    message.success('Created successfully.');
    return result;
  } catch (error) {
    message.error('Failed to create user profile.');
    throw error;
  }
}

export async function Delete(id: number | string) {
  try {
    await api.deleteIdentityAccountInfosId(id);
    message.success('Deleted successfully.');
  } catch (error) {
    message.error('Failed to delete user profile.');
    throw error;
  }
}

export async function Update(
  id: number | string,
  params: UpdateAccountInfoCommand,
) {
  try {
    await api.putIdentityAccountInfosId(id, params);
    message.success('Updated successfully.');
  } catch (error) {
    message.error('Failed to update user profile.');
    throw error;
  }
}

export async function Search(params: SearchAccountInfosQuery) {
  try {
    const result = await api.postIdentityAccountInfosSearch(params);
    return result;
  } catch (error) {
    message.error('Failed to search user profiles.');
    throw error;
  }
}
