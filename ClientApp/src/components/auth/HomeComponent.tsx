import React from 'react';
import { Link } from 'react-router-dom';
import { IHomeProps, IClaim, IUser } from './types';

export default class HomeComponent extends React.Component<IHomeProps> {
  componentDidMount() {
    this.props.getAll();
  }

  handleDeleteUser = (username: string, event: React.MouseEvent<HTMLButtonElement, MouseEvent>) => {
    event.preventDefault();
    // event.persist(); // this stores parameters like id and value inside the event.target
    this.props.delete(username);
  };

  render(): JSX.Element {
    const { logon, users } = this.props;
    return (
      <>
        <h4 className="title is-size-4 has-text-dark">Hi {logon.user.username}!</h4>

        <h4 className="title is-size-4 has-text-dark">Claims</h4>
        <div className="panel">
          {logon.claims.map((claim: IClaim) => (
            <div className="panel-block" key={claim.value}>
              <span className="panel-icon">
                <i className="fas fa-key" aria-hidden="true" />
              </span>
              {claim.value}
            </div>
          ))}
        </div>

        <h4 className="title is-size-4 has-text-dark">Users</h4>
        {users.loading && (
          <>
            <p className="title is-5 has-text-success">Loading users...</p>
            <progress className="progress progress is-small is-dark" max="100" />
          </>
        )}

        {users.items && (
          <div className="columns is-multiline">
            {users.items.map((user: IUser) => (
              <div className="column is-6-tablet is-12-mobile" key={user.id}>
                <div className="card">
                  <div className="card-header">
                    <p className="card-header-title is-centered">
                      <span className="icon is-medium">
                        <i className="fas fa-user fa-2x" />
                      </span>
                    </p>
                  </div>
                  <div className="card-content">
                    <div className="content">
                      <h5 className="title is-5 has-text-centered">{user.username}</h5>
                      <div className="level">
                        {user.email && (
                          <div className="level-left">
                            <div className="level-item">
                              <span className="icon has-text-info">
                                <i className="fas fa-envelope" />
                              </span>
                              <p>{user.email}</p>
                            </div>
                          </div>
                        )}
                        {user.phonenumber && (
                          <div className="level-right">
                            <div className="level-item">
                              <span className="icon has-text-info">
                                <i className="fa fa-phone" />
                              </span>
                              <p>{user.phonenumber}</p>
                            </div>
                          </div>
                        )}
                      </div>

                      <div className="buttons is-centered">
                        <button type="button" className="button is-outlined">
                          <span className="icon is-small">
                            <i className="fas fa-user-circle" />
                          </span>
                          <span>View Profile</span>
                        </button>
                        {user.deleting ? (
                          <h6 className="has-text-info is-size-6">Deleting...</h6>
                        ) : user.deleteError ? (
                          <h6 className="has-text-danger is-size-6">{user.deleteError}</h6>
                        ) : (
                          <button
                            type="button"
                            className="button is-danger is-outlined"
                            onClick={(event) => this.handleDeleteUser(user.username, event)}>
                            <span className="icon is-small">
                              <i className="fas fa-times" />
                            </span>
                            <span>Delete</span>
                          </button>
                        )}
                      </div>
                    </div>
                  </div>
                </div>
              </div>
            ))}
          </div>
        )}

        <div className="has-text-centered">
          <Link className="button is-info" to="/login">
            Logout
          </Link>
        </div>
      </>
    );
  }
}
