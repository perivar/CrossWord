import React, { PureComponent } from 'react';
import * as signalR from '@microsoft/signalr';
import { BulmaButton } from '../shared/bulma-components/BulmaButton';

interface IMessage {
  user: string;
  message: string;
}

interface ILocalState {
  connection: signalR.HubConnection | null;
  messages: IMessage[]; // messages should be an array
}

const config = { apiUrl: process.env.REACT_APP_API };

export default class StatusComponent extends PureComponent<any, ILocalState> {
  constructor(props: any) {
    super(props);

    this.state = {
      connection: null,
      messages: []
    };
  }

  componentDidMount() {
    const connection = new signalR.HubConnectionBuilder()
      .withUrl(`${config.apiUrl}/crosswordsignalrhub`)
      .configureLogging(signalR.LogLevel.Information)
      .build();

    this.setState({ connection });

    // listen to Broadcast events
    connection.on('Broadcast', (user, message) => {
      this.setState((state) => ({
        messages: [...state.messages, { user, message }] // Append the messages to the state here
      }));
    });

    connection
      .start()
      .then(() => connection.invoke('BroadcastAll', 'app', 'Started Connection'))
      .catch((err) => console.error(err.toString()));
  }

  componentWillUnmount() {
    this.state.connection!.stop();
  }

  handleSendStatus = () => {
    this.state.connection!.invoke('SendStatus').catch((err) => console.error(err.toString()));
  };

  render() {
    const messages = this.state.messages.map((msg: IMessage) => (
      <tr key={`${msg.user}${msg.message}`}>
        <td>
          <strong>{msg.user}</strong>
        </td>
        <td>{msg.message}</td>
      </tr>
    ));
    return (
      <>
        <div className="box">
          <div className="table-container">
            <table className="table is-bordered is-striped is-narrow is-hoverable is-fullwidth table-container">
              <thead>
                <tr>
                  <th>User</th>
                  <th>Message</th>
                </tr>
              </thead>
              <tbody>{messages}</tbody>
            </table>
          </div>
        </div>
        <BulmaButton
          type="primary"
          label="Send Status"
          id="sendStatus"
          disabled={false}
          handleOnClick={this.handleSendStatus}
        />
      </>
    );
  }
}
