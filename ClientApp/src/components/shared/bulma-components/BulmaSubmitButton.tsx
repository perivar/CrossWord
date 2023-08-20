import React from 'react';

export interface BulmaSubmitButtonArguments {
  text: string;
  loading: boolean;
}

function SubmitButton({ text, loading }: BulmaSubmitButtonArguments) {
  return (
    <div className="field">
      <div className="control">
        <button
          type="submit"
          className={`button is-block is-info is-large is-fullwidth ${loading ? 'is-loading' : ''}`}>
          {text}
        </button>
      </div>
    </div>
  );
}

export const BulmaSubmitButton = React.memo(SubmitButton);
