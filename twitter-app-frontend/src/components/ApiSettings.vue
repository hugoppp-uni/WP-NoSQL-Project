<template>
  <v-container>
    <v-card dir style="margin: auto">

      <v-card-title class="justify-center display-1 font-weight-bold mb-0"
      >Settings
      </v-card-title
      >
      <v-card-text class="text-left subtitle-1 mb-0 pb-0"
      > Before you search for Hashtags please enter some basic settings.
      </v-card-text>
      <v-select
          class="pl-4 mb-0 pb-0 mr-5"
          v-model="userInput.selectedLanguage"
          :items="languageOptions"
          :menu-props="{ top: true, offsetY: true }"
          label="Your language"
          @change="publishUpdatedSettings()"
      ></v-select>
      <validation-provider
          name="TopCountCheck"
          rules="required|between:2,42|numeric"
          v-slot="{ errors }"
      >
        <v-text-field
            class="pl-4 mb-0 pb-0 mr-5"
            v-model="userInput.topCount"
            label="How many trending Hashtags?"
            hint="Must be a positive integer"
            :error-messages="errors"
            @change="publishUpdatedSettings()"
        ></v-text-field>
      </validation-provider>
      <validation-provider
          name="TopCountCheck"
          rules="required|min_value:1|numeric"
          v-slot="{ errors }"
      >
        <v-text-field
            class="pl-4 mb-1 pb-0 mr-5"
            v-model="userInput.lastDays"
            label="How many days into the past?"
            hint="Must be a positive integer"
            :error-messages="errors"
            @change="publishUpdatedSettings()"
        ></v-text-field>
      </validation-provider>

    </v-card>
  </v-container>
</template>

<script>
import {ValidationProvider} from 'vee-validate';
import {required, min_value, max, between, numeric} from 'vee-validate/dist/rules';
import {extend} from 'vee-validate';
import eventBus from '../eventBus'

extend('required', {
  ...required,
  message: 'Field must not be empty',
});

extend('min_value', {
  ...min_value,
  message: 'Min 1 required',
});

extend('max', {
  ...max,
  message: 'Not more than {length} characters allowed',
});

extend('between', {
  ...between,
  message: 'Only numbers between {min} and {max} are allowed',
});

extend('numeric', {
  ...numeric,
  message: 'Only numeric numbers are allowed',
});


export default {
  name: 'ApiSettings',
  components: {
    ValidationProvider,
  },
  data: function () {
    return {
      languageOptions: [
        {value: 'en', text: 'English'},
        {value: 'ja', text: 'Japanese'},
        {value: 'de', text: 'German'},
        {value: 'al', text: 'All available'},
      ],
      userInput: [
        {
          selectedLanguage: '',
          lastDays: '',
          topCount: ''
        }
      ]
    };
  },
  methods: {
    singleWord(value) {
      if (value.length === 0) {
        return true;
      }
      return 'This is required';
    },
    publishUpdatedSettings() {
      eventBus.$emit('userInputUpdated',this.userInput) // if ChildComponent is mounted, we will have a message in the console
    }
  }
}
</script>
