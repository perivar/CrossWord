export interface IHomeProps {
  users: IUserState;
  logon: ILogon;
  getAll: () => void;
  delete: (id: string) => void;
}

export interface ILoginProps {
  username: string;
  password: string;
  submitted: boolean;
  loggingIn: boolean;
  login: (username: string, password: string) => void;
  logout: () => void;
}

export interface ILoginState {
  username: string;
  password: string;
  submitted: boolean;
  remember: boolean;
}

export interface IRegisterProps {
  register: (user: IUser) => void;
  registering?: boolean;
  submitted: boolean;
}

export interface IRegisterState {
  user: IUser;
  submitted: boolean;
  registering?: boolean; // only used by the reducer
  passwordsNotEqual?: boolean; // only used by the register page
  emailsNotEqual?: boolean; // only used by the register page
}

export interface IAlertState {
  className: string;
  message: string;
}

export interface IUserState {
  items: IUser[];
  loading: boolean;
  error: string;
}

export interface IAuthState {
  loggingIn: boolean;
  loggedIn: boolean;
  logon: ILogon;
  logonUserName: string;
  tokenIsValid: boolean;
  pendingRefreshingToken: boolean | null;
}

export interface IUser {
  id: string;
  username: string;
  password: string;
  confirmPassword: string;
  email: string;
  confirmEmail: string;
  phonenumber: string;
  error?: boolean;
  deleting?: boolean;
  deleteError?: string;
}

export interface IClaim {
  issuer: string;
  originalIssuer: string;
  properties: any;
  subject: string;
  type: string;
  value: string;
  valueType: string;
}

export interface ILogon {
  user: IUser;
  claims: IClaim[];
  token: string;
  refreshToken: string;
}

export interface ASPCoreIdentityErrors {
  code: string;
  description: string;
}
