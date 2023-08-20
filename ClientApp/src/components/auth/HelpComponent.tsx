/* eslint-disable class-methods-use-this */
import React, { PureComponent } from 'react';
import { Link } from 'react-router-dom';

export default class HelpComponent extends PureComponent<any, any> {
  render(): JSX.Element {
    return (
      <>
        <p className="subtitle has-text-grey has-text-centered">Hjelp?</p>
        <div className="container">
          <div className="columns is-centered">
            <div className="column is-5-tablet is-5-desktop is-4-widescreen">
              <div className="box">
                <div className="field has-text-centered">
                  <i className="fas fa-question fa-3x" />
                </div>
                <p className="has-text-grey">
                  <Link to="/register">
                    <i className="fas fa-user-plus" />
                    &nbsp; Er du ikke bruker? Register deg nå!
                  </Link>
                </p>
                <p className="has-text-grey">
                  <Link to="/login">
                    <i className="fas fa-user" />
                    &nbsp; Har du allerede bruker? Logg inn!
                  </Link>
                </p>
                <p className="has-text-grey">
                  <Link to="/forgotten-password">
                    <i className="fa fa-lock" />
                    &nbsp; Glemt passord?
                  </Link>
                </p>
              </div>
            </div>
          </div>
        </div>
      </>
    );
  }
}
