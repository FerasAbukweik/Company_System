import { ValidatorFn } from '@angular/forms';
import { regexPatterns } from '../constants/regexPatterns';

const validators = {
  email: (v: string) =>
    ((!v || !v.trim()) && 'Email is required') ||
    (!regexPatterns.email.test(v) && 'Wrong Email Address') ||
    '',

  loginPassword: (v: string) => ((!v || !v.trim()) && 'Password is required') || '',

  signupPassword: (v: string) =>
    ((!v || !v.trim()) && 'Password is required') ||
    (!regexPatterns.password.test(v) && 'Weak Password') ||
    '',

  phoneNumber: (v: string) =>
    ((!v || !v.trim()) && 'Phone Number is required') ||
    (!regexPatterns.JODPhoneNumber.test(v) && 'Wrong Phone Number') ||
    '',
};

function validate(toValidate: keyof typeof validators): ValidatorFn {
  return (control) => {
    const value = control.value;
    const error = validators[toValidate](value);

    return error ? { [error]: true } : null;
  };
}

export const customValidator = {
  email: validate('email'),
  loginPassword: validate('loginPassword'),
  signupPassword: validate('signupPassword'),
  phoneNumber: validate('phoneNumber'),
};
