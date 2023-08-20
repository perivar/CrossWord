import React, { useState } from 'react';

export type BulmaConfirmButtonType = 'primary' | 'link' | 'info' | 'success' | 'warning' | 'danger';

export interface BulmaConfirmButtonState {
  confirming: boolean;
}

export interface BulmaConfirmButtonArguments {
  type: BulmaConfirmButtonType;
  label: string;
  confirmLabel: string;
  key: string;
  disabled?: boolean;
  loading?: boolean;
  handleOnClick: (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => void;
}

const TIMEOUT = 2000;

function ConfirmButton(props: BulmaConfirmButtonArguments) {
  const { type, label, confirmLabel, key, disabled = false, loading = false, handleOnClick } = props;

  const [confirming, setConfirming] = useState<boolean>();

  const handleLocalOnClick = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
    if (confirming) {
      setConfirming(false);
      if (handleOnClick) handleOnClick(e);
    } else {
      setConfirming(true);
      setTimeout(() => {
        setConfirming(false);
      }, TIMEOUT);
    }
  };

  const confirmIcon = (
    <span className="icon is-small">
      <i className="fas fa-check" />
    </span>
  );

  const localLabel = confirming ? confirmLabel : label;
  const localClassNames = `button${` is-${type}`}${confirming ? ` is-focused` : ''}${loading ? ' is-loading' : ''}`;

  return (
    <button
      key={key}
      type="button"
      className={localClassNames}
      disabled={disabled}
      aria-label={localLabel}
      onClick={handleLocalOnClick}>
      {confirming && confirmIcon}
      <span>{localLabel}</span>
    </button>
  );
}

// export const BulmaConfirmButton = React.memo(ConfirmButton);
export const BulmaConfirmButton = ConfirmButton;
