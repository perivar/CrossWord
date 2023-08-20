import React, { ChangeEvent } from 'react';

interface CheckboxProps {
  name: string;
  label?: string;
  isSelected: boolean;
  onCheckboxChange: (changeEvent: ChangeEvent<HTMLInputElement>) => void;
}

function Checkbox({ name, label, isSelected, onCheckboxChange }: CheckboxProps) {
  return (
    <label className="checkbox" htmlFor={name}>
      <input type="checkbox" name={name} id={name} checked={isSelected} onChange={onCheckboxChange} />
      {label}
    </label>
  );
}

export default Checkbox;
