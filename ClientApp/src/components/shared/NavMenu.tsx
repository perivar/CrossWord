import React, { Component } from 'react';
import { NavLink } from 'react-router-dom';

interface ILocalState {
  active: boolean;
}

export class NavMenu extends Component<{}, ILocalState> {
  // ref variables
  // private menu: React.RefObject<HTMLDivElement>;
  // private hamburger: React.RefObject<HTMLButtonElement>;

  constructor(props: any) {
    super(props);

    // this.menu = React.createRef();
    // this.hamburger = React.createRef();

    this.state = { active: false };
  }

  handleNavBurgerToggle = () => {
    // if (this.hamburger.current) {
    //   if (this.hamburger.current.classList.contains('is-active')) {
    //     this.hamburger.current.classList.remove('is-active');
    //   } else {
    //     this.hamburger.current.classList.add('is-active');
    //   }
    // }
    // if (this.menu.current) {
    //   if (this.menu.current.classList.contains('is-active')) {
    //     this.menu.current.classList.remove('is-active');
    //   } else {
    //     this.menu.current.classList.add('is-active');
    //   }
    // }

    this.setState((prevState) => ({
      active: !prevState.active
    }));
  };

  handleNavBurgerClose = () => {
    // if (this.hamburger.current) {
    //   if (this.hamburger.current.classList.contains('is-active')) {
    //     this.hamburger.current.classList.remove('is-active');
    //   }
    // }
    // if (this.menu.current) {
    //   if (this.menu.current.classList.contains('is-active')) {
    //     this.menu.current.classList.remove('is-active');
    //   }
    // }

    this.setState({ active: false });
  };

  public render() {
    const { active } = this.state;
    return (
      <nav className="navbar has-shadow">
        <div className="container">
          <div className="navbar-brand">
            <NavLink className="navbar-item" to="/" onClick={this.handleNavBurgerClose}>
              <h4 className="title is-4">CrosswordFeud</h4>
            </NavLink>
            <button
              type="button"
              className={`navbar-burger button is-white ${active && 'is-active'}`}
              aria-label="menu"
              aria-expanded="false"
              data-target="navbarBasic"
              onClick={this.handleNavBurgerToggle}
              // ref={this.hamburger}
            >
              <span aria-hidden="true" />
              <span aria-hidden="true" />
              <span aria-hidden="true" />
            </button>
          </div>

          <div
            id="navbarBasic"
            className={`navbar-menu ${active && 'is-active'}`}
            // ref={this.menu}
          >
            <div className="navbar-start">
              <NavLink
                className="navbar-item is-tab"
                to="/dictionary"
                activeClassName="is-active"
                onClick={this.handleNavBurgerClose}>
                Dictionary
              </NavLink>
              {/* <NavLink
                  className="navbar-item is-tab"
                  to="/counter"
                  activeClassName="is-active"
                  onClick={this.handleNavBurgerClose}>
                  Counter
                </NavLink> */}
              <NavLink
                className="navbar-item is-tab"
                to="/crossword"
                activeClassName="is-active"
                onClick={this.handleNavBurgerClose}>
                Crossword
              </NavLink>
              <NavLink
                className="navbar-item is-tab"
                to="/status"
                activeClassName="is-active"
                onClick={this.handleNavBurgerClose}>
                Status
              </NavLink>
              {/* <NavLink
                  className="navbar-item is-tab"
                  to="/forecast"
                  activeClassName="is-active"
                  onClick={this.handleNavBurgerClose}>
                  Forecast
                </NavLink> */}
            </div>
            <div className="navbar-end">
              <div className="navbar-item">
                <div className="buttons">
                  <NavLink
                    className="button is-info"
                    to="/register"
                    activeClassName="is-active"
                    onClick={this.handleNavBurgerClose}>
                    <strong>Sign up</strong>
                  </NavLink>
                  <NavLink
                    className="button is-light"
                    to="/login"
                    activeClassName="is-active"
                    onClick={this.handleNavBurgerClose}>
                    Login
                  </NavLink>
                </div>
              </div>
            </div>
          </div>
        </div>
      </nav>
    );
  }
}
