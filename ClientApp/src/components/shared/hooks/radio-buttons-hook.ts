import { useState } from 'react';

function useRadioButtons<T>(name: string, initialValue: T): any {
  const [value, setState] = useState<T>(initialValue);

  const handleChange = (e: any) => {
    setState(e.target.value);
  };

  const inputProps = {
    name,
    type: 'radio',
    onChange: handleChange
  };

  return { value, inputProps };
}

export default useRadioButtons;
