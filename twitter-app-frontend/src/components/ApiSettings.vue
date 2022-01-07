<template>
  <v-container>
    <v-card dir style="margin: auto">

      <v-card-title class="justify-center display-1 font-weight-bold mb-3"
      >Settings
      </v-card-title
      >
      <v-card-text class="text-left subtitle-1"
      > Before you search for Hashtags please enter some basic settings.
      </v-card-text>
      <v-text-field
          class="pa-0 pl-4"
          v-model="userInput.hashtag"
          label="Your interesting Hashtag"
          counter="20"
          maxlength="20"
          hint="Must be a single word"
      ></v-text-field>
      <v-select
          class="pa-0 pl-4"
          v-model="userInput.selectedLanguage"
          :items="languageOptions"
          :menu-props="{ top: true, offsetY: true }"
          label="Your language"
      ></v-select>
      <validation-provider
          name="TopCountCheck"
          rules="required|between:1,42|numeric"
          v-slot="{ errors }"
      >
        <v-text-field
            class="pa-0 pl-4"
            v-model="userInput.topCount"
            label="How many trending Hashtags?"
            hint="Must be a positive integer"
            :error-messages="errors"
        ></v-text-field>
      </validation-provider>
      <validation-provider
          name="TopCountCheck"
          rules="required|min_value:1|numeric"
          v-slot="{ errors }"
      >
        <v-text-field
            class="pa-0 pl-4"
            v-model="userInput.lastDays"
            label="How many days into the past?"
            hint="Must be a positive integer"
            :error-messages="errors"
        ></v-text-field>
      </validation-provider>
    </v-card>
  </v-container>
</template>

<script>
import {ValidationProvider} from 'vee-validate';
import {required, min_value, max, between, numeric} from 'vee-validate/dist/rules';
import {extend} from 'vee-validate';

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
        {value: 'eng', text: 'English'},
        {value: 'ja', text: 'Japanese'},
        {value: 'de', text: 'German'},
        {value: 'all', text: 'All available'}, //TODO: Treate differently with API call
      ],
      userInput: [
        {
          hashtag: '',
          selectedLanguage: 'eng', //TODO: Check why default value doesn't work (maybe just can't be accessed because of shallow copies?)
          lastDays: 365,
          topCount: 10
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
  }
}
</script>
