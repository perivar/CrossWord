import React from 'react';
import { Link } from 'react-router-dom';
import { ILoginProps, ILoginState } from './types';
import { BulmaInputField } from '../shared/bulma-components/BulmaInputField';
import { BulmaSubmitButton } from '../shared/bulma-components/BulmaSubmitButton';
import { BulmaCheckboxField } from '../shared/bulma-components/BulmaCheckboxField';

export default class LoginComponent extends React.Component<ILoginProps, ILoginState> {
  constructor(props: ILoginProps) {
    super(props);

    // reset login status
    this.props.logout();

    this.state = {
      username: '',
      password: '',
      submitted: false,
      remember: false
    };
  }

  handleChange = (e: React.ChangeEvent<HTMLInputElement>): void => {
    const { name, value } = e.target;
    this.setState((prevState) => ({
      ...prevState,
      [name]: value
    }));
  };

  handleCheckboxChange = (): void => {
    this.setState((prevState) => ({
      ...prevState,
      remember: !prevState.remember
    }));
  };

  handleSubmit = (e: React.FormEvent): void => {
    e.preventDefault();

    this.setState({ submitted: true });
    const { username, password } = this.state;
    if (username && password) {
      this.props.login(username, password);
    }
  };

  render(): JSX.Element {
    const { loggingIn } = this.props;
    const { username, password, submitted, remember } = this.state;
    return (
      <>
        <p className="subtitle has-text-grey has-text-centered">Logg inn</p>
        <div className="container">
          <div className="columns is-centered">
            <div className="column is-5-tablet is-5-desktop is-4-widescreen">
              <form className="box" onSubmit={this.handleSubmit}>
                <div className="field has-text-centered">
                  <i className="fas fa-sign-in-alt fa-3x" />
                </div>
                <BulmaInputField
                  label="Brukernavn"
                  type="text"
                  name="username"
                  placeholder="feks. ola@nordmann.no"
                  required
                  requiredMessage="Gyldig brukernavn (e-post adresse) er påkrevd"
                  value={username}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fas fa-user" />}
                />
                <BulmaInputField
                  label="Passord"
                  type="password"
                  name="password"
                  placeholder="*********"
                  required
                  requiredMessage="Gyldig passord er påkrevd"
                  value={password}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-lock" />}
                />
                <BulmaCheckboxField
                  label="Husk meg"
                  name="remember"
                  checked={remember}
                  handleChange={this.handleCheckboxChange}
                />
                <BulmaSubmitButton text="Log inn" loading={loggingIn} />
              </form>
              <p className="has-text-grey">
                <Link to="/register">
                  <i className="fas fa-user-plus" />
                  &nbsp; Er du ikke bruker? Register deg nå!
                </Link>
              </p>
              <p className="has-text-grey">
                <Link to="/forgotten-password">
                  <i className="fa fa-lock" />
                  &nbsp; Glemt passord?
                </Link>
              </p>
              <p className="has-text-grey">
                <Link to="/help">
                  <i className="fas fa-question" />
                  &nbsp; Trenger du hjelp?
                </Link>
              </p>
            </div>
          </div>
        </div>
      </>
    );
  }
}
