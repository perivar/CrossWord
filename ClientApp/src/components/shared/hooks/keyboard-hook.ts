import { useEffect } from 'react';

export function useKeyboardEvent(key: string, callback: Function): any {
  useEffect(() => {
    const downHandler = (event: KeyboardEvent) => {
      if (event.key === key) {
        callback();
      }
    };

    // Add event listeners
    window.addEventListener('keydown', downHandler);

    // Remove event listeners on cleanup
    return () => {
      window.removeEventListener('keydown', downHandler);
    };
  }, [callback, key]);
}
