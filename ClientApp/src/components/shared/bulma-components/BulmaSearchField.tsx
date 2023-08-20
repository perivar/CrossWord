import React, { useEffect } from 'react';

interface SearchFieldProps {
  type: 'addon' | 'grouped';
  label: string;
  placeholder: string;
  value: string;
  handleSubmit: (filterQuery: string) => void;
}

// BulmaSearchField
function SearchField(props: SearchFieldProps) {
  const { type, label, placeholder, value, handleSubmit } = props;

  const searchInputRef: React.RefObject<HTMLInputElement> = React.createRef();

  useEffect(() => {
    if (searchInputRef && searchInputRef.current) searchInputRef.current.value = value;
  }, [searchInputRef, value]);

  const handleSearchKeyPress = (e: React.KeyboardEvent<HTMLInputElement>) => {
    const keyCode = e.keyCode || e.which;

    if (keyCode === 13) {
      e.preventDefault();
      doSearch();
    }
  };

  const handleSearchSubmit = (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    doSearch();
  };

  const handleSearchClear = (e: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
    e.preventDefault();
    if (searchInputRef && searchInputRef.current) searchInputRef.current.value = '';
    doSearch();
  };

  const doSearch = () => {
    if (searchInputRef && searchInputRef.current) {
      handleSubmit(searchInputRef.current.value);
    }
  };

  return (
    <form onSubmit={handleSearchSubmit}>
      <div className={`field ${type === 'addon' ? 'has-addons' : type === 'grouped' ? 'is-grouped' : ''}`}>
        <div className="control is-expanded has-icons-right">
          <input
            ref={searchInputRef}
            className="input"
            type="text"
            placeholder={placeholder}
            onKeyPress={handleSearchKeyPress}
          />
          <button type="button" className="icon is-small is-right is-icon-button" onClick={handleSearchClear}>
            <i className="fas fa-times fa-xs" />
          </button>
        </div>
        <p className="control">
          <button type="submit" className="button">
            {label}
          </button>
        </p>
      </div>
    </form>
  );
}

export const BulmaSearchField = React.memo(SearchField);
