import { useRef } from 'react';

export const useFocus = (): any => {
  const htmlElRef = useRef<HTMLElement>();
  const setFocus = () => {
    const currentEl = htmlElRef.current;
    if (currentEl) currentEl.focus();
  };
  return [setFocus, htmlElRef] as const;
};
