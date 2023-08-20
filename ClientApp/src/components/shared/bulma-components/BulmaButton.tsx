import React from 'react';

export type BulmaButtonType = 'primary' | 'link' | 'info' | 'success' | 'warning' | 'danger';

export interface BulmaButtonArguments {
  type: BulmaButtonType;
  label: string;
  id: string;
  disabled?: boolean;
  loading?: boolean;
  handleOnClick: (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => void;
}

function Button(props: BulmaButtonArguments) {
  const { type, label, id, disabled = false, loading = false, handleOnClick } = props;

  return (
    <button
      key={id}
      type="button"
      className={`button is-${type}${loading ? ' is-loading' : ''}`}
      disabled={disabled}
      aria-label={label}
      onClick={handleOnClick}>
      <span>{label}</span>
    </button>
  );
}

// export const BulmaButton = React.memo(Button);
export const BulmaButton = Button;
