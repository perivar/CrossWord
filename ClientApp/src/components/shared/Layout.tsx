import React, { PureComponent } from 'react';
import { NavMenu } from './NavMenu';

export class Layout extends PureComponent<any, any> {
  public render(): JSX.Element {
    return (
      <section className="hero">
        <div className="hero-head">
          <NavMenu />
        </div>
        <div className="hero-body">
          <div className="container">{this.props.children}</div>
        </div>
      </section>
    );
  }
}
