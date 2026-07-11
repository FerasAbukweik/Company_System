import { Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { ILoginForm } from './login.model';
import { customValidator } from '../../core/validators/custom-validator';
import { KeyValuePipe, NgClass } from '@angular/common';
import { LoginDTO } from '../../core/dto/login-dto';
import { AuthService } from '../../core/services/client/auth-service';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, NgClass],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {
  // DI
  private readonly _authService = inject(AuthService);

  // protected properties

  // login form
  protected loginForm = new FormGroup<ILoginForm>({
    email: new FormControl('', {
      nonNullable: true,
      validators: [customValidator.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [customValidator.loginPassword],
    }),
  });

  // signals
  protected isPasswordVisible = signal<boolean>(false);
  protected serverError = signal<string>('');

  // methods

  // check if controle have error
  isError(controlName: keyof ILoginForm) {
    let control = this.loginForm.controls[controlName];
    return control.invalid && control.touched;
  }

  // updateErrors errors
  getError(type: keyof ILoginForm) {
    if (!this.isError(type)) return '';

    return Object.keys(this.loginForm.controls[type].errors!)[0] || '';
  }

  getErrors() {
    let errors: string[] = [];

    Object.keys(this.loginForm.controls).forEach((key) => {
      errors.push(this.getError(key as keyof ILoginForm));
    });

    return errors;
  }

  // toggle passowrd
  togglePassword() {
    this.isPasswordVisible.update((curr) => !curr);
  }

  // on submit
  async onSubmit() {
    // to show errors for user
    this.loginForm.markAllAsTouched();

    // if form is invalid stop
    if (this.loginForm.invalid) return;

    const loginData: LoginDTO = {
      email: this.loginForm.controls.email.value,
      password: this.loginForm.controls.password.value,
    };

    // login
    this._authService.login(loginData).subscribe({
      next: () => {
        this.serverError.set('');
      },
      error: (err) => {
        this.serverError.set(err.error || err.error.message || 'unexpected error');
      },
    });
  }
}
