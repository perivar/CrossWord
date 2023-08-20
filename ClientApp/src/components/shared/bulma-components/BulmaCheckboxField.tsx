import React from 'react';

export interface BulmaChexkboxFieldArguments {
  label: string;
  name: string;
  checked: boolean;
  handleChange: (e: React.ChangeEvent<HTMLInputElement>) => void;
  checkboxProps?: any;
}

function CheckboxField(props: BulmaChexkboxFieldArguments) {
  const { label, name, checked, handleChange, checkboxProps } = props;
  return (
    <div className="field">
      <div className="control">
        <label htmlFor={name} className="checkbox">
          <input type="checkbox" id={name} name={name} onChange={handleChange} checked={checked} {...checkboxProps} />
          {label}
        </label>
      </div>
    </div>
  );
}

export const BulmaCheckboxField = React.memo(CheckboxField);
// export const BulmaCheckboxField = CheckboxField;
