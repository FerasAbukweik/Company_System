import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { PositionsEnum } from '../../../../../core/enum/positions-enum';
import { HeroIcon } from '../../../../../core/constants/hero-icon-d';
import { customValidator } from '../../../../../core/validators/custom-validator';
import { validateImageSize } from '../../../../../core/utils/file-utils';
import { ToastService } from '../../../../../core/services/api-services/toast-service';
import { AvatarComponent } from '../../../../../shared/components/avatar/avatar.component';
import { IconComponent } from '../../../../../shared/components/icon/icon.component';
import { DrawerComponent } from '../../../../../shared/components/drawer/drawer.component';
import { EmployeeParentPickerComponent } from '../employee-parent-picker/employee-parent-picker.component';
import { EmployeeDTO } from '../../../../../core/dto/users/employee-dto';
import { EmployeeFormValue, ASSIGNABLE_POSITION_OPTIONS } from '../../employees.model';

const MAX_IMAGE_SIZE = 5 * 1024 * 1024;

interface EmployeeForm {
  fullName: FormControl<string>;
  userName: FormControl<string>;
  email: FormControl<string>;
  phone: FormControl<string>;
  password: FormControl<string>;
  parentId: FormControl<string>;
  position: FormControl<PositionsEnum>;
  hourlyRate: FormControl<number | null>;
  annualHolidayAllowance: FormControl<number | null>;
}

/**
 * Slide-over used for BOTH adding and editing an employee.
 * `employee() === null` means "create", otherwise "edit that employee".
 */
@Component({
  selector: 'app-employee-drawer',
  imports: [
    ReactiveFormsModule,
    AvatarComponent,
    DrawerComponent,
    IconComponent,
    EmployeeParentPickerComponent,
  ],
  templateUrl: './employee-drawer.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class EmployeeDrawerComponent {
  protected readonly HeroIcon = HeroIcon;
  protected readonly positionOptions = ASSIGNABLE_POSITION_OPTIONS;

  // DI
  private readonly toastService = inject(ToastService);

  // inputs
  open = input.required<boolean>();
  employee = input<EmployeeDTO | null>(null);
  isSaving = input<boolean>(false);
  serverError = input<string>('');

  // outputs
  closed = output<void>();
  saved = output<EmployeeFormValue>();

  // signals
  protected readonly imageFile = signal<File | null>(null);
  protected readonly imagePreview = signal<string | null>(null);
  protected readonly isImageError = signal(false);

  // computed
  protected readonly isEdit = computed(() => this.employee() !== null);
  protected readonly avatarUrl = computed(
    () => this.imagePreview() ?? this.employee()?.imageUrl ?? null,
  );
  // form
  protected readonly form = new FormGroup<EmployeeForm>({
    fullName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    userName: new FormControl('', { nonNullable: true, validators: [Validators.required] }),
    email: new FormControl('', { nonNullable: true, validators: [customValidator.email] }),
    phone: new FormControl('', { nonNullable: true, validators: [customValidator.phoneNumber] }),
    // password and parentId are only validated when creating (see loadForm)
    password: new FormControl('', { nonNullable: true }),
    parentId: new FormControl('', { nonNullable: true }),
    position: new FormControl(PositionsEnum.Employee, { nonNullable: true }),
    hourlyRate: new FormControl<number | null>(null, { validators: [Validators.min(0)] }),
    annualHolidayAllowance: new FormControl<number | null>(null, {
      validators: [Validators.min(0), Validators.max(366)],
    }),
  });

  constructor() {
    // (re)fill the form every time the drawer is opened
    effect(() => {
      if (!this.open()) return;
      const employee = this.employee();
      untracked(() => this.loadForm(employee));
    });

    // preview of a freshly picked image
    effect((onCleanup) => {
      const file = this.imageFile();
      if (!file) {
        this.imagePreview.set(null);
        return;
      }
      const url = URL.createObjectURL(file);
      this.imagePreview.set(url);
      onCleanup(() => URL.revokeObjectURL(url));
    });
  }

  // methods
  protected close() {
    if (this.open()) this.closed.emit();
  }

  protected isError(name: keyof EmployeeForm) {
    const control = this.form.controls[name];
    return control.invalid && control.touched;
  }

  protected onImageSelected(event: Event) {
    const element = event.target as HTMLInputElement;
    const file = element.files?.[0];
    element.value = ''; // allow picking the same file again
    if (!file) return;

    const result = validateImageSize(file, MAX_IMAGE_SIZE);
    if (result === 'too large') return this.toastService.error('Maximum Image Size Is 5MB');
    if (result === 'wrong type') return this.toastService.error('Choose a PNG, JPG or WebP image.');

    this.imageFile.set(file);
    this.isImageError.set(false);
  }

  protected selectParent(parentId: string) {
    this.form.controls.parentId.setValue(parentId);
    this.form.controls.parentId.markAsTouched();
  }

  protected onSubmit() {
    this.form.markAllAsTouched();
    // creating requires a profile picture (same rule as the old add-employee page)
    if (!this.isEdit() && !this.imageFile()) this.isImageError.set(true);

    if (this.form.invalid || this.isSaving() || (!this.isEdit() && !this.imageFile())) return;

    this.saved.emit({
      ...this.form.getRawValue(),
      image: this.imageFile(),
    });
  }

  private loadForm(employee: EmployeeDTO | null) {
    this.form.reset({
      fullName: employee?.fullName ?? '',
      userName: employee?.userName ?? '',
      email: employee?.email ?? '',
      phone: employee?.phone ?? '',
      password: '',
      parentId: employee?.parentId ?? '',
      position: employee?.position ?? PositionsEnum.Employee,
      hourlyRate: employee?.hourlyRate ?? null,
      annualHolidayAllowance: employee?.annualHolidayAllowance ?? null,
    });

    // password and parent are only needed when creating an account
    const { password, parentId } = this.form.controls;
    password.setValidators(employee ? [] : [customValidator.signupPassword]);
    parentId.setValidators(employee ? [] : [Validators.required]);
    password.updateValueAndValidity();
    parentId.updateValueAndValidity();

    this.imageFile.set(null);
    this.isImageError.set(false);
  }
}
