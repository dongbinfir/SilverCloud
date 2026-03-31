import { message } from 'antd';
import {
  getWebAPIV1,
  type GetUserProfileQuery,
  type CreateUserProfileCommand,
  type UpdateUserProfileCommand,
  type SearchUserProfilesQuery,
} from '@user-api';

const api = getWebAPIV1();

export async function Get(params: GetUserProfileQuery) {
  try {
    const result = await api.postUserUserProfilesGet(params);
    return result;
  } catch (error) {
    message.error('Failed to get user profile.');
    throw error;
  }
}

export async function Create(params: CreateUserProfileCommand) {
  try {
    const result = await api.postUserUserProfiles(params);
    message.success('Created successfully.');
    return result;
  } catch (error) {
    message.error('Failed to create user profile.');
    throw error;
  }
}

export async function Delete(id: number | string) {
  try {
    await api.deleteUserUserProfilesId(id);
    message.success('Deleted successfully.');
  } catch (error) {
    message.error('Failed to delete user profile.');
    throw error;
  }
}

export async function Update(
  id: number | string,
  params: UpdateUserProfileCommand,
) {
  try {
    await api.putUserUserProfilesId(id, params);
    message.success('Updated successfully.');
  } catch (error) {
    message.error('Failed to update user profile.');
    throw error;
  }
}

export async function Search(params: SearchUserProfilesQuery) {
  try {
    const result = await api.postUserUserProfilesSearch(params);
    return result;
  } catch (error) {
    message.error('Failed to search user profiles.');
    throw error;
  }
}
