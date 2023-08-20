import React from 'react';
import { Link } from 'react-router-dom';
import { IRegisterProps, IRegisterState } from './types';
import { BulmaInputField } from '../shared/bulma-components/BulmaInputField';
import { BulmaSubmitButton } from '../shared/bulma-components/BulmaSubmitButton';

export default class RegisterComponent extends React.Component<IRegisterProps, IRegisterState> {
  constructor(props: IRegisterProps) {
    super(props);

    this.state = {
      user: {
        id: '',
        username: '',
        password: '',
        confirmPassword: '',
        email: '',
        confirmEmail: '',
        phonenumber: ''
      },
      submitted: false,
      passwordsNotEqual: false,
      emailsNotEqual: false
    };
  }

  handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    const { user } = this.state;
    this.setState({
      user: {
        ...user,
        [name]: value
      }
    });
  };

  handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();

    // perform all neccassary validations
    const { user } = this.state;
    const { password, confirmPassword, email, confirmEmail } = user;
    if (password !== confirmPassword) {
      this.setState({
        user: {
          ...user,
          confirmPassword: ''
        },
        passwordsNotEqual: true,
        submitted: true
      });
    } else if (email !== confirmEmail) {
      this.setState({
        user: {
          ...user,
          confirmEmail: ''
        },
        emailsNotEqual: true,
        submitted: true
      });
    } else {
      // make API call
      if (user.username && user.password && user.email) {
        this.props.register(user);
      }
      this.setState({ submitted: true });
    }
  };

  render() {
    const { registering } = this.props;
    const { user, submitted, passwordsNotEqual, emailsNotEqual } = this.state;
    return (
      <>
        <p className="subtitle has-text-grey has-text-centered">Registrer deg som ny bruker</p>
        <div className="container">
          <div className="columns is-centered">
            <div className="column is-5-tablet is-5-desktop is-4-widescreen">
              <form className="box" onSubmit={this.handleSubmit}>
                <div className="field has-text-centered">
                  <i className="fas fa-user-plus fa-3x" />
                </div>
                <BulmaInputField
                  label="Brukernavn"
                  type="text"
                  name="username"
                  placeholder="feks. ola@nordmann.no"
                  required
                  requiredMessage="Gyldig brukernavn (e-post adresse) er påkrevd"
                  value={user.username}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fas fa-user" />}
                />
                <BulmaInputField
                  label="E-post adresse"
                  type="email"
                  name="email"
                  placeholder="feks. ola@nordmann.no"
                  required
                  requiredMessage="Gyldig e-post adresse er påkrevd"
                  value={user.email}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-envelope" />}
                />
                <BulmaInputField
                  label="Bekreft e-post adresse"
                  type="email"
                  name="confirmEmail"
                  placeholder="feks. ola@nordmann.no"
                  required
                  requiredMessage={emailsNotEqual ? 'E-post adressene er ulike' : 'Gyldig e-post adresse er påkrevd'}
                  value={user.confirmEmail}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-envelope" />}
                />
                <BulmaInputField
                  label="Telefon nummer"
                  type="text"
                  name="phonenumber"
                  placeholder="feks. +47 40506070"
                  required={false}
                  requiredMessage="Gyldig telefon nummer er påkrevd"
                  value={user.phonenumber}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-phone" />}
                />

                <div className="is-divider" data-content="Passord" />

                <BulmaInputField
                  label="Passord"
                  type="password"
                  name="password"
                  placeholder="*********"
                  required
                  requiredMessage="Gyldig passord er påkrevd"
                  value={user.password}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-lock" />}
                />

                <BulmaInputField
                  label="Bekreft Passord"
                  type="password"
                  name="confirmPassword"
                  placeholder="*********"
                  required
                  requiredMessage={passwordsNotEqual ? 'Passordene er ulike' : 'Gyldig passord er påkrevd'}
                  value={user.confirmPassword}
                  submitted={submitted}
                  handleChange={this.handleChange}
                  icon={<i className="fa fa-lock" />}
                />
                <BulmaSubmitButton text="Register ny bruker" loading={registering!} />
              </form>
              <p className="has-text-grey">
                <Link to="/login">
                  <i className="fas fa-user" />
                  &nbsp; Har du allerede bruker? Logg inn!
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
