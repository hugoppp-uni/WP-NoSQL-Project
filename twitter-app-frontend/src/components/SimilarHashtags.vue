<template>
  <v-container>
    <div class="text-center">
      <v-btn
          class="ma-2"
          outlined
          color="white"
          @click="getSimilarHashtags"
      >
        Show Similar Hashtags
      </v-btn>
<!--    Similar Hashtags section:-->
    <v-card
        v-if="similarHashtagsNotEmpty"
        class="mx-auto"
        max-width="300"
        tile
    >
      <v-list rounded>
        <v-list-item-group
            color="primary"
        >
          <v-list-item
              v-for="(item, index) in similarHashtagsResult"
              :key="index"
          >
            <v-list-item-content>
              <v-list-item-title v-text="item.hashtag"></v-list-item-title>
            </v-list-item-content>
          </v-list-item>
        </v-list-item-group>
      </v-list>
    </v-card>
      <div class="text-center">
        <v-btn
            class="ma-2"
            outlined
            color="white"
            @click="getTopicsOfHashtags"
        >
          Show Topics of Hashtag
        </v-btn>
      </div>
    </div>
    <!--    Topics of Hashtags section:-->
    <v-card
        v-if="this.topicsOfHashtagsNotEmpty"
        class="mx-auto"
        max-width="300"
        tile
    >
      <v-list rounded>
        <v-list-item-group
            color="primary"
        >
          <v-list-item
              v-for="(item, index) in topicOfHashtagsResult"
              :key="index"
          >
            <v-list-item-content>
              <v-list-item-title v-text="item.topic"></v-list-item-title>
            </v-list-item-content>
          </v-list-item>
        </v-list-item-group>
      </v-list>
    </v-card>
    <v-text-field
        class="pl-4 pr-4"
        v-model="selectedHashtag"
        label="Your interesting Hashtag"
        counter="20"
        maxlength="20"
        hint="Must be a single word"
    ></v-text-field>
  </v-container>
</template>

<script>


import axios from "axios";
import eventBus from "../eventBus";

export default {
  mounted() {
    // adding eventBus listener
    eventBus.$on('hashtagSelected', (param) => {
      console.log('Hashtag received: ' + param)
      this.selectedHashtag = param
    })
    eventBus.$on('userInputUpdated', (param) => {
      console.log('Lang received: ' + param.selectedLanguage)
      this.selectedLanguage = param.selectedLanguage;
      console.log('LastDays received: ' + param.lastDays)
      this.lastDays = param.lastDays;
    })
  },
  beforeDestroy() {
    // removing eventBus listener
    eventBus.$off('hashtagSelected')
  },
  computed: {
    similarHashtagsNotEmpty: function () {
      return (this.similarHashtagsResult.length > 1)
    },
    topicsOfHashtagsNotEmpty: function () {
      return (this.topicOfHashtagsResult.length > 1)
    }
  },
  data: function () {
    return {
      //input Data from Settings:
      selectedLanguage: '',
      lastDays: '',
      // input Data from input field or Top Hashtags:
      selectedHashtag: '',

      // Api Call Similar Hashtags:
      defaultSimilarHashtagUrl: 'http://localhost:5038/Hashtag/',
      similarHashtagsResult: '',

      // Api Call Topics of Hashtag:
      topicOfHashtagsResult: ''
    };
  },
  methods: {
    getSimilarHashtags() {
      let requestUrl = this.createSimilarHashtagUrl()
      if (requestUrl === this.defaultSimilarHashtagUrl) {
        return
      }
      axios
          .get(requestUrl)
          .then(res => {
            this.similarHashtagsResult = res.data;
            console.log('Api call for: ' + this.selectedHashtag)
            console.log('API response for similar hashtags received ')
          });
    },
    getTopicsOfHashtags() {
      let requestUrl = this.createTopicsOfHashtagUrl()
      if (requestUrl === this.defaultSimilarHashtagUrl) {
        return
      }
      axios
          .get(requestUrl)
          .then(res => {
            this.topicOfHashtagsResult = res.data;
            console.log('Api call for: ' + this.selectedHashtag)
            console.log('API response for topics received ')
          });
    },
    createSimilarHashtagUrl() {
      let requestUrl = this.defaultSimilarHashtagUrl;
      if (this.selectedHashtag === '') {
        console.log("No Hashtag selected!!!!!")
        return requestUrl
      }
      // append selected Hashtag:
      requestUrl += this.selectedHashtag + '/similar'


      // Append selected language:
      let languageAppended = false;
      if (this.selectedLanguage !== undefined && this.selectedLanguage !== '' && this.selectedLanguage !== 'al') {
        requestUrl += '?language=' + this.selectedLanguage
        languageAppended = true;
      }
      // Append selected LastDays:
      if (this.lastDays !== undefined && this.lastDays !== '') {
        if (languageAppended) {
          requestUrl += '&';
        } else {
          requestUrl += '?';
        }
        requestUrl += 'lastDays=' + this.lastDays
      }
      console.log("Request URL: " + requestUrl)
      return requestUrl;
    },
    createTopicsOfHashtagUrl() {
      let requestUrl = this.defaultSimilarHashtagUrl;
      // Stop if Hashtag is empty:
      if (this.selectedHashtag === '') {
        console.log("No Hashtag selected!!!!!")
        return requestUrl
      }
      // append selected Hashtag:
      requestUrl += this.selectedHashtag + '/topics'

      // Append selected language:
      if (this.selectedLanguage !== undefined && this.selectedLanguage !== '' && this.selectedLanguage !== 'al') {
        requestUrl += '?language=' + this.selectedLanguage
      }
      console.log("Request URL: " + requestUrl)
      return requestUrl;
    }
  }
}
</script>
