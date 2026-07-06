import { ValidatorFn } from "@angular/forms";
import { regexPatterns } from "../../core/constants/regexPatterns";

const validators = {
    email: (v: string) => ((!v || !v.trim()) && "Email is required") || (!regexPatterns.email.test(v) && "Invalid email format") || "",
    password: (v: string) => ((!v || !v.trim()) && "Password is required") || "",
}


function validate(toValidate: keyof typeof validators): ValidatorFn{
    return (control) => {
        const value = control.value;
        const error = validators[toValidate](value);

        return error ? { [error]: true } : null;
    };
}


export const loginValidators = {
    email: validate("email"),
    password: validate("password"),
}