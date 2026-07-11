import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ReactiveFormsModule, FormGroup, Validators, FormControl } from '@angular/forms';
import { PositionsEnum } from '../../core/enum/positions-enum';
import { customValidator } from '../../core/validators/custom-validator';
import { ToastService } from '../../core/services/client/toast-service';
import { AddEmployeeDTO } from '../../core/dto/create-account-dto';
import { AccountService } from '../../core/services/client/account-service';
import { OrgTreeService } from '../../core/services/client/org-tree-service';
import { IsVisableDirective } from '../../shared/directives/is-visable.directive';
import { LoadingComponent } from '../../shared/components/loading/loading.component';

interface RegisterForm {
  userName: FormControl<string>;
  fullName: FormControl<string>;
  email: FormControl<string>;
  phone: FormControl<string>;
  position: FormControl<PositionsEnum>;
  password: FormControl<string>;
  parentId: FormControl<string>;
}

@Component({
  selector: 'app-employee-registration',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IsVisableDirective, LoadingComponent],
  templateUrl: './add-employee.component.html',
})
export class AddEmployeeComponent {
  // DI
  private readonly toastService = inject(ToastService);
  private readonly accountService = inject(AccountService);
  protected readonly orgTreeService = inject(OrgTreeService);

  // signals
  protected isPasswordVisible = signal<boolean>(false);
  protected isImageError = signal<boolean>(false);
  protected selectedImageFile = signal<File | null>(null);
  protected serverError = signal<string>('');

  // computed
  protected selectedImage = computed(() => {
    const image = this.selectedImageFile();

    if (!image) return 'user_vector.jpg';

    return URL.createObjectURL(image);
  });

  // form
  public registrationForm: FormGroup<RegisterForm> = new FormGroup<RegisterForm>({
    userName: new FormControl('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', {
      nonNullable: true,
      validators: [customValidator.email],
    }),
    phone: new FormControl('', {
      nonNullable: true,
      validators: [customValidator.phoneNumber],
    }),
    position: new FormControl<PositionsEnum>(PositionsEnum.Employee, {
      nonNullable: true,
      validators: [Validators.required],
    }),
    password: new FormControl('', {
      nonNullable: true,
      validators: [customValidator.signupPassword],
    }),
    parentId: new FormControl<string>('', {
      nonNullable: true,
      validators: [Validators.required],
    }),
  });

  // methods

  reset() {
    this.registrationForm.reset();
    this.isImageError.set(false);
    this.selectedImageFile.set(null);
    this.isPasswordVisible.set(false);
    this.serverError.set('');
  }

  isError(controlName: keyof RegisterForm) {
    const control = this.registrationForm.controls[controlName];

    return control.invalid && control.touched;
  }

  getError(controlName: keyof RegisterForm) {
    const control = this.registrationForm.controls[controlName];

    return Object.keys(control.errors || {})[0] || '';
  }

  togglePassword() {
    this.isPasswordVisible.update((visible) => !visible);
  }

  onImageSelected(event: Event) {
    const target = event.target as HTMLInputElement;

    if (!target.files || !target.files.length) return;

    const maxImageSize = 5 * 1024 * 1024;
    const image = target.files[0];
    if (image.size > maxImageSize) {
      this.toastService.error('Maximum Image Size Is 5MB');
      return;
    }

    this.selectedImageFile.set(image);
    this.isImageError.set(false);
  }

  selectParent(parentId: string) {
    this.registrationForm.patchValue({ parentId: parentId });

    const control = this.registrationForm.controls['parentId'];
    if (control) {
      control.updateValueAndValidity();

      console.log(control);
    }
  }

  async onSubmit() {
    // to update ui
    this.registrationForm.markAllAsTouched();
    if (!this.selectedImageFile()) this.isImageError.set(true);

    // if any error stop
    if (this.registrationForm.invalid || !this.selectedImageFile()) return;

    const form = this.registrationForm.getRawValue();

    const toAddEmployee: AddEmployeeDTO = {
      email: form.email,
      fullName: form.fullName,
      password: form.password,
      phoneNumber: form.phone,
      position: form.position,
      userName: form.userName,
      parentId: form.parentId,
      image: this.selectedImageFile()!,
    };

    this.toastService.info('adding employee');

    // send request to add employee
    this.accountService.addEmployee(toAddEmployee).subscribe({
      next: () => {
        this.reset();
      },
      error: (err) => {
        const errorMessaege: string = err.error || err.error.message || 'Unexpected Error';
        this.serverError.set(errorMessaege);

        setTimeout(() => {
          this.serverError.set('')
        }, 5000);
      },
    });
  }
}
