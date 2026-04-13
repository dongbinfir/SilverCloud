import { Card, Descriptions, Space, Tag, Typography } from 'antd';
import { useAuthStore } from '@store/authStore';

const ProfilesPage = () => {
  const accountName = useAuthStore((state) => state.accountName);
  const accountId = useAuthStore((state) => state.accountId);
  const email = useAuthStore((state) => state.email);
  const phoneNum = useAuthStore((state) => state.phoneNum);

  return (
    <Space direction="vertical" size={16} style={{ display: 'flex' }}>
      <Card title="用户资料">
        <Descriptions column={1} size="small">
          <Descriptions.Item label="用户名称">{accountName ?? '-'}</Descriptions.Item>
          <Descriptions.Item label="用户编号">{accountId ?? '-'}</Descriptions.Item>
          <Descriptions.Item label="邮箱">{email ?? '-'}</Descriptions.Item>
          <Descriptions.Item label="手机号">{phoneNum ?? '-'}</Descriptions.Item>
        </Descriptions>
      </Card>
      <Card>
        <Typography.Paragraph type="secondary">
          这里先作为用户资料页占位，后续可以接入 AccountInfos API 或表格查询。
        </Typography.Paragraph>
        <Tag color="blue">Placeholder</Tag>
      </Card>
    </Space>
  );
};

export default ProfilesPage;