import { GuardianCrossword } from 'mycrossword';

export interface ICrosswordContainerState {
  data: GuardianCrossword;
  loading: boolean;
  error: string;
}
