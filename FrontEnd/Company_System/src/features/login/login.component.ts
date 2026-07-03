import { Component, DestroyRef, inject, signal } from '@angular/core';
import { FormControl, FormGroup, FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ILoginForm } from './login.model';
import { loginValidators } from './login.validation';
import { KeyValuePipe, NgClass } from '@angular/common';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/api/AuthService';
import { LoginDTO } from '../../core/dto/auth/login-dto';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

@Component({
  selector: 'app-login',
  imports: [ReactiveFormsModule, NgClass, KeyValuePipe],
  templateUrl: './login.component.html',
  styleUrl: './login.component.css',
})
export class LoginComponent {  
  // DI
  private readonly _router = inject(Router);
  private readonly _authService = inject(AuthService);
  private readonly _destroyRef = inject(DestroyRef);



  // protected properties

  // login form
  protected loginForm = new FormGroup<ILoginForm>({
    email: new FormControl('', {
      nonNullable: true,
      validators: [loginValidators.email],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [loginValidators.password],
    }),
  });


  // signals
  protected isPasswordVisible = signal<boolean>(false);

  protected errors = signal({
    email: "",
    password: "",
    server: ""  
  });
  



  // methods

  // check if controle have error
  isError(controlName: keyof ILoginForm){
    let control = this.loginForm.controls[controlName];
    return control.invalid && control.touched && control.dirty 
  }

  // updateErrors errors
  updateError(type: keyof ILoginForm){
    // if error add the error
    if(this.isError(type)){
      const error = Object.keys(this.loginForm.controls[type].errors!)[0] || "";
      this.errors.update(curr => ({ ...curr, [type]: error }));
    }
    // if no error remove the error
    else{
      this.errors.update(curr => ({ ...curr, [type]: "" }));
    }

    // remove server error if any
    this.errors.update(curr => ({ ...curr, server: "" }));
  }


  // toggle passowrd
  togglePassword(){
    this.isPasswordVisible.update(curr => !curr);
  }

  // on submit
  onSubmit() {
    // if any error stop
    if(Object.values(this.errors()).some(error => !!error)) return;

    // try to login
    const loginData: LoginDTO = {
      email: this.loginForm.controls.email.value,
      password: this.loginForm.controls.password.value,
    }

    this._authService.login(loginData)
    .pipe(takeUntilDestroyed(this._destroyRef))
    .subscribe({
      next: () => {
        this._router.navigateByUrl("/");
      },
      error: (err)=>{
        this.errors.update(curr => ({ ...curr, server: err.error || err.error.message || "Server Error" }));

        console.log(err);
      }
    })
  }

}
