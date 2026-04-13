import { Card, List, Tag, Typography } from 'antd';

const mockPermissions = [
  '登录认证',
  '访问控制',
  '角色配置',
  'Refresh Token 管理',
];

const AuthorizationsPage = () => {
  return (
    <Card title="权限管理" extra={<Tag color="processing">规划中</Tag>}>
      <Typography.Paragraph type="secondary">
        当前页面用于承接权限与认证相关模块。先用静态内容搭建导航入口，后续可以继续扩展为权限列表、角色管理和接口授权配置。
      </Typography.Paragraph>
      <List
        bordered
        dataSource={mockPermissions}
        renderItem={(item) => <List.Item>{item}</List.Item>}
      />
    </Card>
  );
};

export default AuthorizationsPage;