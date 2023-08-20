import React from 'react';

export type BulmaNotificationType = 'primary' | 'link' | 'info' | 'success' | 'warning' | 'danger';

interface NotificationProps {
  type: BulmaNotificationType;
  visible: boolean;
  setVisible: (value: boolean) => void;
  message: string;
  handleClick?: (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => void;
}

// BulmaNotification
function Notification(props: NotificationProps) {
  const { type, visible, setVisible, message, handleClick } = props;

  const handleOnClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
    e.preventDefault();
    setVisible(false);
    if (handleClick) handleClick(e);
  };

  if (visible) {
    return (
      <div className={`notification is-${type}`}>
        <button type="button" className="delete" onClick={handleOnClick} />
        {message}
      </div>
    );
  }
  return <div />;
}

export const BulmaNotification = React.memo(Notification);
