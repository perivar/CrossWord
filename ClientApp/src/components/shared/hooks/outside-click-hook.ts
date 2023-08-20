import { useEffect } from 'react';

export function useOutsideClick(ref: any, callback: Function): any {
  // React.RefObject<T> doesn't work because it's missing contains
  const handleClick = (event: MouseEvent) => {
    if (ref.current && !ref.current.contains(event.target)) {
      callback();
    }
  };

  useEffect(() => {
    document.addEventListener('click', handleClick);

    return () => {
      document.removeEventListener('click', handleClick);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);
}
